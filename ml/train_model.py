from pathlib import Path
import argparse, json, sys
import numpy as np
import pandas as pd
import joblib

FEATURES = ["commandedSpeed", "conveyorLoad", "motorCurrent", "temp", "vibration"]
BEARINGS = ["ROLLER-1BEARING-1", "ROLLER-1BEARING-2", "ROLLER-2BEARING-1", "ROLLER-2BEARING-2"]
WINDOW_SIZE, SEED, EXPECTED_SCENARIOS = 30, 42, 7


def label_scenarios(df, expected):
    required = {"timestamp", "bearingID", "isMotorRunning"}
    missing = required - set(df.columns)
    if missing:
        raise ValueError(f"Missing required columns: {sorted(missing)}")
    df = df.copy()
    df["timestamp"] = pd.to_datetime(df["timestamp"], errors="raise")
    state = pd.to_numeric(df["isMotorRunning"], errors="coerce")
    if state.isna().any() or not state.isin([0, 1]).all():
        raise ValueError("isMotorRunning must contain only numeric 0/1 values, without missing values.")
    df["isMotorRunning"] = state.astype(int)
    ts = df[["timestamp", "isMotorRunning"]].drop_duplicates().sort_values("timestamp", kind="stable")
    if ts["timestamp"].duplicated().any():
        raise ValueError("Conflicting isMotorRunning states exist for the same timestamp.")
    ts = ts.reset_index(drop=True)
    starts_mask = ts["isMotorRunning"].eq(1) & ts["isMotorRunning"].shift(1, fill_value=0).eq(0)
    starts = ts.loc[starts_mask, "timestamp"].tolist()
    if len(starts) != expected:
        raise ValueError(f"Expected {expected} scenario starts (0->1 transitions), found {len(starts)}. Check isMotorRunning data.")
    start_to_id = {pd.Timestamp(t): i + 1 for i, t in enumerate(starts)}
    sid, phase, active = {}, {}, 0
    for t, s in zip(ts["timestamp"], ts["isMotorRunning"]):
        t = pd.Timestamp(t)
        if t in start_to_id:
            active = start_to_id[t]
        if active:
            sid[t] = active
            phase[t] = "running" if int(s) == 1 else "cooling"
    df["scenario_id"] = df["timestamp"].map(sid)
    df["phase"] = df["timestamp"].map(phase)
    df = df[df["scenario_id"].notna()].copy()
    df["scenario_id"] = df["scenario_id"].astype(int)
    counts = df.groupby(["scenario_id", "phase"]).size().unstack(fill_value=0)
    for p in ("running", "cooling"):
        if p not in counts.columns:
            counts[p] = 0
    return df, starts, counts.sort_index()


def split_groups(df, window):
    parts = {"train": [], "validation": [], "test": []}
    too_short = []
    for keys, g in df.groupby(["scenario_id", "phase", "bearingID"], sort=True):
        g = g.sort_values("timestamp", kind="stable").reset_index(drop=True)
        n = len(g); a = int(n * .70); b = int(n * .15); c = n - a - b
        if min(a, b, c) < window:
            too_short.append((keys, n, a, b, c)); continue
        parts["train"].append(g.iloc[:a].copy())
        parts["validation"].append(g.iloc[a:a+b].copy())
        parts["test"].append(g.iloc[a+b:].copy())
    if too_short:
        detail = "\n".join(f"{k}: total={n}, train={a}, validation={b}, test={c}" for k,n,a,b,c in too_short[:15])
        raise ValueError(f"{len(too_short)} scenario/phase/bearing group(s) are too short for {window}-step windows in every split.\n{detail}\nCheck data or reduce --window-size; groups are not silently dropped.")
    return {k: pd.concat(v, ignore_index=True) for k,v in parts.items()}


def make_windows(df, scaler, window):
    X, meta = [], []
    scaled = df.copy()
    scaled[FEATURES] = scaler.transform(scaled[FEATURES]).astype(np.float32)
    for keys, g in scaled.groupby(["scenario_id", "phase", "bearingID"], sort=True):
        g = g.sort_values("timestamp", kind="stable").reset_index(drop=True)
        vals = g[FEATURES].to_numpy(np.float32); times = g["timestamp"].to_numpy()
        scenario, phase, bearing = keys
        for i in range(max(0, len(vals) - window + 1)):
            X.append(vals[i:i+window])
            meta.append({"scenario_id": int(scenario), "phase": str(phase), "bearingID": str(bearing),
                         "window_start": pd.Timestamp(times[i]), "window_end": pd.Timestamp(times[i+window-1])})
    if not X:
        return np.empty((0, window, len(FEATURES)), dtype=np.float32), pd.DataFrame(meta)
    return np.stack(X).astype(np.float32), pd.DataFrame(meta)


def reconstruction_errors(model, X, batch):
    y = model.predict(X, batch_size=batch, verbose=0)
    return np.mean(np.square(X-y), axis=(1,2))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--input", default="conveyor_bearing_dataset_cleaned.csv")
    ap.add_argument("--output-dir", default="conveyor_model_artifacts")
    ap.add_argument("--window-size", type=int, default=WINDOW_SIZE)
    ap.add_argument("--epochs", type=int, default=50)
    ap.add_argument("--batch-size", type=int, default=128)
    ap.add_argument("--threshold-percentile", type=float, default=99.0)
    ap.add_argument("--expected-scenarios", type=int, default=EXPECTED_SCENARIOS)
    args = ap.parse_args()
    if args.window_size < 2 or not 0 < args.threshold_percentile < 100:
        sys.exit("Use --window-size >= 2 and --threshold-percentile between 0 and 100.")
    try:
        import tensorflow as tf
        from tensorflow.keras import layers, Model
        from tensorflow.keras.callbacks import EarlyStopping, ModelCheckpoint
    except ImportError:
        sys.exit("TensorFlow is missing. Activate your ml/.venv and install the required dependencies.")
    path = Path(args.input).expanduser(); out = Path(args.output_dir).expanduser(); out.mkdir(parents=True, exist_ok=True)
    if not path.exists(): sys.exit(f"Dataset not found: {path.resolve()}")
    df = pd.read_csv(path)
    required = {"timestamp", "bearingID", "isMotorRunning", *FEATURES}
    missing = required - set(df.columns)
    if missing: sys.exit(f"Dataset missing columns: {sorted(missing)}")
    try: df, starts, counts = label_scenarios(df, args.expected_scenarios)
    except Exception as e: sys.exit(f"Scenario detection failed: {e}")
    for f in FEATURES: df[f] = pd.to_numeric(df[f], errors="coerce")
    bad = df[["timestamp", "bearingID", *FEATURES]].isna().sum()
    if bad.any():
        print("Missing values (nothing was dropped or imputed):\n", bad[bad > 0].to_string())
        sys.exit("Fix missing timestamps, bearing IDs, or feature values before training.")
    if not np.isfinite(df[FEATURES].to_numpy(np.float64)).all(): sys.exit("Feature values include inf/-inf.")
    if set(df["bearingID"].astype(str).unique()) != set(BEARINGS): print("WARNING: Bearing IDs differ from the expected four IDs:", sorted(df["bearingID"].astype(str).unique()))
    print(f"Rows used: {len(df):,}; scenarios detected: {len(starts)}")
    for i,t in enumerate(starts, 1): print(f"  Scenario {i} starts: {pd.Timestamp(t)}")
    print("Rows by scenario/phase:\n", counts.to_string())
    try: parts = split_groups(df, args.window_size)
    except Exception as e: sys.exit(f"Chronological split failed: {e}")
    from sklearn.preprocessing import StandardScaler
    scaler = StandardScaler().fit(parts["train"][FEATURES])
    windows = {k: make_windows(v, scaler, args.window_size) for k,v in parts.items()}
    for k,(X,_) in windows.items():
        if len(X) == 0: sys.exit(f"No {k} windows generated; check dataset length/window size.")
        print(f"{k}: rows={len(parts[k]):,}; windows={len(X):,}")
    Xtr, _ = windows["train"]; Xv, _ = windows["validation"]; Xte, _ = windows["test"]
    print("Feature order:", FEATURES)
    print("Scaler means:", [float(x) for x in scaler.mean_])
    print("Scaler scales:", [float(x) for x in scaler.scale_])
    tf.keras.utils.set_random_seed(SEED)
    inp = layers.Input(shape=(args.window_size, len(FEATURES)), name="telemetry")
    x = layers.LSTM(32, return_sequences=True, name="encoder_lstm_1")(inp)
    z = layers.LSTM(16, return_sequences=False, name="encoder_lstm_2")(x)
    x = layers.RepeatVector(args.window_size, name="repeat_latent")(z)
    x = layers.LSTM(16, return_sequences=True, name="decoder_lstm_1")(x)
    x = layers.LSTM(32, return_sequences=True, name="decoder_lstm_2")(x)
    out_layer = layers.TimeDistributed(layers.Dense(len(FEATURES)), name="reconstruction")(x)
    model = Model(inp, out_layer, name="conveyor_lstm_autoencoder")
    model.compile(optimizer=tf.keras.optimizers.Adam(learning_rate=.001), loss="mse")
    best = out / "best_lstm_autoencoder.keras"
    callbacks = [EarlyStopping(monitor="val_loss", patience=5, min_delta=.0001, restore_best_weights=True, verbose=1),
                 ModelCheckpoint(str(best), monitor="val_loss", save_best_only=True, verbose=1)]
    hist = model.fit(Xtr, Xtr, validation_data=(Xv, Xv), epochs=args.epochs,
                     batch_size=args.batch_size, shuffle=True, callbacks=callbacks, verbose=1)
    model.save(out / "lstm_autoencoder.keras")
    model.save_weights(out / "lstm_autoencoder.weights.h5")
    joblib.dump(scaler, out / "standard_scaler.joblib")
    errs = {k: reconstruction_errors(model, X, args.batch_size) for k,(X,_) in windows.items()}
    threshold = float(np.percentile(errs["validation"], args.threshold_percentile))
    print(f"\nValidation p{args.threshold_percentile:g} anomaly threshold: {threshold:.10g}")
    for k,e in errs.items():
        n = int((e > threshold).sum())
        print(f"{k}: n={len(e):,}, mean_error={e.mean():.8g}, p95={np.percentile(e,95):.8g}, flagged={n} ({100*n/len(e):.2f}%)")
    scaler_json = {"features": FEATURES, "feature_count": len(FEATURES),
                   "means": [float(x) for x in scaler.mean_], "scales": [float(x) for x in scaler.scale_],
                   "formula": "scaled_value = (raw_value - mean) / scale", "dtype": "float32"}
    (out/"scaler_parameters.json").write_text(json.dumps(scaler_json, indent=4), encoding="utf-8")
    config = {"model_file":"lstm_autoencoder.onnx", "keras_model_file":"lstm_autoencoder.keras",
              "weights_file":"lstm_autoencoder.weights.h5", "scaler_file":"standard_scaler.joblib",
              "scaler_parameters_file":"scaler_parameters.json", "features":FEATURES,
              "feature_count":len(FEATURES), "window_size":args.window_size,
              "input_shape":[None,args.window_size,len(FEATURES)], "input_dtype":"float32",
              "anomaly_threshold":threshold, "threshold_percentile":args.threshold_percentile,
              "threshold_method":f"{args.threshold_percentile:g}th percentile of validation reconstruction errors",
              "reconstruction_error":"mean squared error across all timesteps and features",
              "scenario_count":len(starts), "scenario_detection":"timestamp-level isMotorRunning transition 0->1",
              "phase_mapping":{"isMotorRunning=1":"running", "isMotorRunning=0 after first scenario start":"cooling"},
              "split":{"method":"chronological within scenario_id + phase + bearingID", "train":.70,"validation":.15,"test":.15},
              "training_rows":len(parts["train"]),"validation_rows":len(parts["validation"]),"test_rows":len(parts["test"]),
              "training_feature_min":{f:float(parts["train"][f].min()) for f in FEATURES},
              "training_feature_max":{f:float(parts["train"][f].max()) for f in FEATURES},
              "note":"Validation reconstruction errors set the threshold. Flags indicate unusual patterns, not confirmed physical faults."}
    config_path = out/"deployment_config.json"
    config_path.write_text(json.dumps(config, indent=4), encoding="utf-8")
    try:
        import tf2onnx, onnx
        sig = (tf.TensorSpec((None,args.window_size,len(FEATURES)),tf.float32,name="telemetry"),)
        onnx_path = out/"lstm_autoencoder.onnx"
        tf2onnx.convert.from_keras(model, input_signature=sig, opset=17, output_path=str(onnx_path))
        onnx.checker.check_model(str(onnx_path))
        import onnxruntime as ort
        sess = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
        sample = Xte[:min(16,len(Xte))]
        y_tf = model.predict(sample, verbose=0)
        y_onnx = sess.run([sess.get_outputs()[0].name], {sess.get_inputs()[0].name:sample.astype(np.float32)})[0]
        diff = float(np.max(np.abs(y_tf-y_onnx)))
        config["onnx_max_abs_difference"] = diff
        config["onnx_validation_passed"] = bool(np.allclose(y_tf,y_onnx,rtol=1e-3,atol=1e-4))
        config_path.write_text(json.dumps(config,indent=4),encoding="utf-8")
        print("ONNX export validated; max absolute TF/ONNX difference:", diff)
        if not config["onnx_validation_passed"]: print("WARNING: ONNX output differs from TensorFlow beyond tolerance.")
    except Exception as e:
        config["onnx_validation_passed"] = False; config["onnx_export_error"] = repr(e)
        config_path.write_text(json.dumps(config,indent=4),encoding="utf-8")
        print("WARNING: ONNX export failed; Keras model, weights and scaler files were saved.\n", repr(e))
        print("Unity needs ONNX; repair/install tf2onnx, onnx and onnxruntime, then rerun training.")
    all_results=[]
    for name,(X,meta) in windows.items():
        r=meta.copy(); r["partition"]=name; r["reconstruction_error"]=errs[name]; r["is_anomaly"]=errs[name]>threshold; all_results.append(r)
    pd.concat(all_results,ignore_index=True).to_csv(out/"window_anomaly_scores.csv",index=False)
    pd.DataFrame(hist.history).to_csv(out/"training_history.csv",index_label="epoch")
    counts.to_csv(out/"scenario_phase_row_counts.csv",index_label="scenario_id")
    print("\nArtifacts saved to:",out.resolve())
    for p in sorted(out.iterdir()):
        if p.is_file(): print(f"  {p.name}: {p.stat().st_size:,} bytes")

if __name__ == "__main__": main()
