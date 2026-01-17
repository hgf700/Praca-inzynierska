import pandas as pd
import numpy as np
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Dense
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.callbacks import EarlyStopping
from sklearn.preprocessing import StandardScaler
import joblib

# --- Dane ---
#wczytanie
file = "genetic_Result"
path = f"example_data_csv/{file}.csv"
data = pd.read_csv(path)
epoki=1

# --- Kolumny wejściowe i wyjściowe ---
X_cols = ["day","shift","preference","requirements","singleWorkerFitness"]
Y_cols = ["assigned"]

X = data[X_cols].values
Y = data[Y_cols].values

# --- Normalizacja singleWorkerFitness ---
scaler = StandardScaler()
X[:, 4] = scaler.fit_transform(X[:, 4].reshape(-1,1)).flatten()

# --- Model ---
model = Sequential([
    Dense(64, activation='relu', input_shape=(X.shape[1],)),
    Dense(32, activation='relu'),
    Dense(1, activation='sigmoid')
])

# --- Early stopping ---
early_stop = EarlyStopping(
    monitor='val_loss',
    patience=5,
    restore_best_weights=True
)

model.compile(optimizer=Adam(), loss='binary_crossentropy', metrics=['accuracy'])

# --- Trening ---
model.fit(X, Y, epochs=epoki, batch_size=32, validation_split=0.2, callbacks=[early_stop])

# --- Test predykcji na kilku pierwszych wierszach ---
pred = model.predict(X[:10])
print("Predykcja (pierwsze 10):")
print(np.hstack([pred, Y[:10]]))

# --- Zapis modelu i scalera ---
model.save("saved_model/model.keras")
joblib.dump(scaler, "saved_model/scaler.save")
