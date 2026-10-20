from pathlib import Path
import argparse
import sys
import pandas as pd
import numpy as np

BEARING_MAP = {
    "ROLLER-1BEARING-1": {
        "temp": "roller_1_bearing_1_temp",
        "vibration": "roller_1_bearing_1_vibration",
    },
    "ROLLER-1BEARING-2": {
        "temp": "roller_1_bearing_2_temp",
        "vibration": "roller_1_bearing_2_vibration",
    },
    "ROLLER-2BEARING-1": {
        "temp": "roller_2_bearing_1_temp",
        "vibration": "roller_2_bearing_1_vibration",
    },
    "ROLLER-2BEARING-2": {
        "temp": "roller_2_bearing_2_temp",
        "vibration": "roller_2_bearing_2_vibration",
    },
}
SHARED_TAGS = ["isMotorRunning", "commandedSpeed", "conveyorLoad", "motorCurrent"]
FEATURES = ["commandedSpeed", "conveyorLoad", "motorCurrent", "temp", "vibration"]
MAIN_COLUMNS = [
    "bearingID", "timestamp", "isMotorRunning", "commandedSpeed",
    "conveyorLoad", "motorCurrent", "temp", "vibration"
]

def prepare_from_long(raw: pd.DataFrame, tolerance_ms: float) -> pd.DataFrame:
    required = {"timestamp", "tagName", "value"}
    if not required.issubset(raw.columns):
        raise ValueError(f"Long-format CSV must contain columns: {sorted(required)}")
    raw = raw.copy()
    raw["timestamp"] = pd.to_datetime(raw["timestamp"], errors="raise")
    raw["tagName"] = raw["tagName"].astype(str).str.strip()
    raw["value"] = pd.to_numeric(raw["value"], errors="coerce")

    # Use every commandedSpeed sample as the reference timeline, as in the original workflow.
    reference = (
        raw.loc[raw["tagName"].eq("commandedSpeed"), ["timestamp", "value"]]
        .sort_values("timestamp", kind="stable")
        .rename(columns={"value": "commandedSpeed"})
        .reset_index(drop=True)
    )
    if reference.empty:
        raise ValueError("No rows with tagName='commandedSpeed' were found.")
    if reference["timestamp"].duplicated().any():
        # Duplicate timestamps are retained by using a stable sequential sample index;
        # each reference event remains represented.
        reference["reference_event"] = np.arange(len(reference))
    else:
        reference["reference_event"] = np.arange(len(reference))

    tolerance = pd.Timedelta(milliseconds=tolerance_ms)

    def align_tag(tag_name: str, output_name: str) -> pd.DataFrame:
        data = (
            raw.loc[raw["tagName"].eq(tag_name), ["timestamp", "value"]]
            .sort_values("timestamp", kind="stable")
            .rename(columns={"timestamp": "source_timestamp", "value": output_name})
            .reset_index(drop=True)
        )
        if data.empty:
            return pd.DataFrame({
                output_name: np.nan,
                f"{output_name}_source_timestamp": pd.NaT,
            }, index=reference.index)
        # merge_asof requires sorted timestamps. It aligns only within the configured
        # tolerance; it does not forward-fill or invent readings.
        left = reference[["timestamp"]].copy()
        left["_row"] = np.arange(len(left))
        matched = pd.merge_asof(
            left.sort_values("timestamp", kind="stable"),
            data.sort_values("source_timestamp", kind="stable"),
            left_on="timestamp", right_on="source_timestamp",
            direction="nearest", tolerance=tolerance,
        ).sort_values("_row")
        matched = matched.set_index("_row").reindex(range(len(reference)))
        return pd.DataFrame({
            output_name: matched[output_name].to_numpy(),
            f"{output_name}_source_timestamp": matched["source_timestamp"].to_numpy(),
        })

    shared = {tag: align_tag(tag, tag) for tag in SHARED_TAGS}
    bearing_frames = []
    for bearing_id, tags in BEARING_MAP.items():
        frame = reference[["timestamp", "reference_event"]].copy()
        for tag in SHARED_TAGS:
            frame[tag] = shared[tag][tag].to_numpy()
        temp = align_tag(tags["temp"], "temp")
        vib = align_tag(tags["vibration"], "vibration")
        frame["temp"] = temp["temp"].to_numpy()
        frame["vibration"] = vib["vibration"].to_numpy()
        frame["temp_source_timestamp"] = temp["temp_source_timestamp"].to_numpy()
        frame["vibration_source_timestamp"] = vib["vibration_source_timestamp"].to_numpy()
        frame["bearingID"] = bearing_id
        bearing_frames.append(frame)
    result = pd.concat(bearing_frames, ignore_index=True)
    return result

def prepare_from_wide(raw: pd.DataFrame) -> pd.DataFrame:
    raw = raw.copy()
    if "timestamp" not in raw.columns:
        raise ValueError("Wide-format CSV must contain a 'timestamp' column.")
    raw["timestamp"] = pd.to_datetime(raw["timestamp"], errors="raise")
    # Historian wide exports can contain repeated timestamps and sparse tag columns.
    # Collapse only duplicate timestamps, taking the last non-null value per column.
    # This is not row deletion by operating phase; it merges duplicate timestamp events.
    raw = raw.sort_values("timestamp", kind="stable")
    value_cols = [c for c in raw.columns if c != "timestamp"]
    raw = raw.groupby("timestamp", as_index=False, sort=True)[value_cols].agg(
        lambda s: s.dropna().iloc[-1] if not s.dropna().empty else np.nan
    )

    required_shared = set(SHARED_TAGS)
    missing_shared = required_shared - set(raw.columns)
    if missing_shared:
        raise ValueError(
            "Wide CSV is missing shared columns: " + ", ".join(sorted(missing_shared))
        )

    frames = []
    for bearing_id, tags in BEARING_MAP.items():
        missing = {tags["temp"], tags["vibration"]} - set(raw.columns)
        if missing:
            raise ValueError(
                f"Wide CSV is missing columns for {bearing_id}: {', '.join(sorted(missing))}"
            )
        frame = raw[["timestamp"] + SHARED_TAGS].copy()
        frame = frame.rename(columns={
            tags["temp"]: "temp",
            tags["vibration"]: "vibration",
        }) if False else frame
        frame["temp"] = pd.to_numeric(raw[tags["temp"]], errors="coerce").to_numpy()
        frame["vibration"] = pd.to_numeric(raw[tags["vibration"]], errors="coerce").to_numpy()
        frame["bearingID"] = bearing_id
        frames.append(frame)
    result = pd.concat(frames, ignore_index=True)
    return result

def main():
    parser = argparse.ArgumentParser(description="Prepare the conveyor bearing dataset without phase filtering.")
    parser.add_argument("--input", required=True, help="Path to original historian CSV (long or wide format).")
    parser.add_argument("--output", default="conveyor_bearing_dataset.csv", help="Output CSV path.")
    parser.add_argument("--tolerance-ms", type=float, default=1.0,
                        help="Timestamp matching tolerance for long-format historian CSV (default: 1 ms).")
    args = parser.parse_args()

    input_path = Path(args.input).expanduser()
    output_path = Path(args.output).expanduser()
    if not input_path.exists():
        sys.exit(f"Input file not found: {input_path}")
    raw = pd.read_csv(input_path)
    print(f"Input: {input_path.resolve()}")
    print(f"Raw shape: {raw.shape}")
    print(f"Raw columns: {raw.columns.tolist()}")

    if {"tagName", "value"}.issubset(raw.columns):
        data = prepare_from_long(raw, args.tolerance_ms)
        input_format = "long"
    elif "timestamp" in raw.columns:
        data = prepare_from_wide(raw)
        input_format = "wide"
    else:
        sys.exit("Could not identify CSV format. Expected either (timestamp, tagName, value) or a wide CSV with timestamp.")

    data["timestamp"] = pd.to_datetime(data["timestamp"], errors="raise")
    data = data.sort_values(["timestamp", "bearingID"], kind="stable").reset_index(drop=True)

    # Do not drop incomplete rows. Report them so data issues remain visible.
    required = MAIN_COLUMNS
    print(f"\nDetected format: {input_format}")
    print(f"Prepared shape: {data.shape}")
    print(f"Expected rows (timestamps × 4 bearings): {data['timestamp'].nunique() * len(BEARING_MAP):,}")
    print(f"Actual rows: {len(data):,}")
    print("\nRows per bearing:")
    print(data["bearingID"].value_counts(dropna=False).sort_index().to_string())
    print("\nMissing values in principal columns (reported, not removed):")
    print(data[required].isna().sum().to_string())
    print("\nTemperature range by bearing:")
    print(data.groupby("bearingID")["temp"].agg(["count", "min", "max", "mean"]).round(3).to_string())
    print("\nTime range:", data["timestamp"].min(), "to", data["timestamp"].max())

    output_path.parent.mkdir(parents=True, exist_ok=True)
    data.to_csv(output_path, index=False)
    print(f"\nSaved dataset: {output_path.resolve()}")
    print("No startup, heating, stopped-state, or cooling rows were filtered by this script.")

if __name__ == "__main__":
    main()
