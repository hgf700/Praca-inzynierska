import pandas as pd
import tensorflow as tf
import os, sys
import joblib 

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
# INPUT_CSV = os.path.join(BASE_DIR, "..", "wyniki", "danetest.csv")

INPUT_CSV = os.path.join(BASE_DIR, "..", "wyniki", "genetic_Result.csv")
MODEL_PATH = os.path.join(BASE_DIR, "..", "saved_model", "model.keras")
SCALER_PATH = os.path.join(BASE_DIR, "..", "saved_model", "scaler.save")
OUTPUT_CSV = os.path.join(BASE_DIR, "..", "wyniki_modelu", "model_Result.csv")

FEATURES = ["day", "shift", "preference", "requirements", "singleWorkerFitness"]

# --- Sprawdzenie plików ---
for p in [INPUT_CSV, MODEL_PATH, SCALER_PATH]:
    if not os.path.exists(p):
        raise FileNotFoundError(f"Brak pliku: {p}")

# --- Wczytanie danych ---
df = pd.read_csv(INPUT_CSV)
X = df[FEATURES].values.astype("float32")

# --- Normalizacja singleWorkerFitness ---
scaler = joblib.load(SCALER_PATH)
X[:, 4] = scaler.transform(X[:, 4].reshape(-1, 1)).flatten()

# --- Wczytanie modelu ---
model = tf.keras.models.load_model(MODEL_PATH)

# --- Predykcja ---
predictions = model.predict(X)

# --- Zapis do CSV ---
df["prediction_flatten"] = predictions.flatten()     
df["prediction_assigned"] = (predictions.flatten() > 0.5).astype(int)

os.makedirs(os.path.dirname(OUTPUT_CSV), exist_ok=True)
df.to_csv(OUTPUT_CSV, index=False)

# --- Logi ---
print("PYTHON:", sys.executable)
print("CWD:", os.getcwd())
print("Model expects:", model.input_shape)
print("X shape:", X.shape)
print("Predictions shape:", predictions.shape)
print("Predykcja zapisana do:", OUTPUT_CSV)
