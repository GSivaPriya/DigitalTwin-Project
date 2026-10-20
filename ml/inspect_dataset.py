"""
inspect_dataset.py
Thoroughly inspect the prepared dataset. This script never changes or filters it.

Example:
  python inspect_dataset.py --input conveyor_bearing_dataset.csv
"""
from pathlib import Path
import argparse
import pandas as pd
import numpy as np

KEY_COLUMNS = ["bearingID", "timestamp", "isMotorRunning", "commandedSpeed",
               "conveyorLoad", "motorCurrent", "temp", "vibration"]
NUMERIC_COLUMNS = ["isMotorRunning", "commandedSpeed", "conveyorLoad",
                   "motorCurrent", "temp", "vibration"]

def main():
    parser = argparse.ArgumentParser(description="Inspect every field in the prepared conveyor dataset.")
    parser.add_argument("--input", default="conveyor_bearing_dataset.csv")
    parser.add_argument("--report", default="dataset_inspection_report.txt")
    args = parser.parse_args()

    path = Path(args.input).expanduser()
    if not path.exists():
        raise SystemExit(f"Dataset not found: {path}. Run prepare_dataset.py first.")
    df = pd.read_csv(path, parse_dates=["timestamp"])
    lines = []
    def out(*items):
        msg = " ".join(str(x) for x in items)
        print(msg)
        lines.append(msg)

    out("DATASET INSPECTION")
    out("=" * 90)
    out("File:", path.resolve())
    out("Shape:", df.shape)
    out("Columns:", df.columns.tolist())
    out("Dtypes:")
    out(df.dtypes.to_string())
    out("\nMissing values by column (no rows removed):")
    out(df.isna().sum().to_string())
    out("\nDuplicate full rows:", int(df.duplicated().sum()))
    if {"timestamp", "bearingID"}.issubset(df.columns):
        out("Duplicate (timestamp, bearingID) keys:",
            int(df.duplicated(["timestamp", "bearingID"]).sum()))
    out("\nTime range:", df["timestamp"].min(), "to", df["timestamp"].max())
    out("Unique timestamps:", df["timestamp"].nunique())
    out("Rows per bearing:")
    out(df["bearingID"].value_counts(dropna=False).sort_index().to_string())

    numeric_present = [c for c in NUMERIC_COLUMNS if c in df.columns]
    out("\nFULL NUMERIC DESCRIPTIVE STATISTICS:")
    out(df[numeric_present].describe(percentiles=[.01, .05, .25, .5, .75, .95, .99]).T.round(6).to_string())

    out("\nMIN/MAX/MEAN/STD BY BEARING:")
    if "bearingID" in df.columns:
        out(df.groupby("bearingID")[numeric_present].agg(["count", "min", "max", "mean", "std"]).round(6).to_string())

    out("\nTEMPERATURE BY MOTOR STATE:")
    if {"temp", "isMotorRunning"}.issubset(df.columns):
        out(df.groupby("isMotorRunning")["temp"].agg(["count", "min", "max", "mean", "std"]).round(6).to_string())

    out("\nALL UNIQUE VALUES (categorical / low-cardinality columns):")
    for col in df.columns:
        if df[col].nunique(dropna=False) <= 50:
            out(f"\n[{col}] unique values ({df[col].nunique(dropna=False)}):")
            out(df[col].value_counts(dropna=False).to_string())

    out("\nNUMERIC EXTREMES:")
    for col in numeric_present:
        out(f"\n[{col}] smallest 10:")
        out(df.nsmallest(10, col)[
    [c for c in ["timestamp", "bearingID", col] if c in df.columns]
])
        out(f"[{col}] largest 10:")
        out(df.nlargest(10, col)[
    [c for c in ["timestamp", "bearingID", col] if c in df.columns]
])

    out("\nTEMPERATURE SEQUENCE CHECK (first/last 20 rows per bearing):")
    for bearing, group in df.sort_values("timestamp").groupby("bearingID", sort=True):
        out(f"\n--- {bearing} | first 20 ---")
        out(group[["timestamp", "isMotorRunning", "commandedSpeed", "conveyorLoad", "temp", "vibration"]].head(20).to_string(index=False))
        out(f"--- {bearing} | last 20 ---")
        out(group[["timestamp", "isMotorRunning", "commandedSpeed", "conveyorLoad", "temp", "vibration"]].tail(20).to_string(index=False))

    out("\nTIME GAPS PER BEARING (seconds; descriptive only):")
    if {"timestamp", "bearingID"}.issubset(df.columns):
        sorted_df = df.sort_values(["bearingID", "timestamp"]).copy()
        sorted_df["gap_seconds"] = sorted_df.groupby("bearingID")["timestamp"].diff().dt.total_seconds()
        out(sorted_df.groupby("bearingID")["gap_seconds"].agg(["count", "min", "median", "max"]).round(6).to_string())
        out("Gaps > 1.5 seconds:", int(sorted_df["gap_seconds"].gt(1.5).sum()))

    report_path = Path(args.report).expanduser()
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text("\n".join(lines), encoding="utf-8")
    out("\nSaved report:", report_path.resolve())
    out("Inspection is read-only: no rows were dropped, imputed, or modified.")

if __name__ == "__main__":
    main()
