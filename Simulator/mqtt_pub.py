import paho.mqtt.client as mqtt
import time
import pandas as pd
import json

# ตั้งค่า MQTT Broker
broker_address = "broker.hivemq.com"
port = 1883
topic = "mfec/energy/sensor"

# โหลดข้อมูล CSV
try:
	df = pd.read_csv("Data/monitor_actuals.csv")
	print("Loaded CSV successfully!")
	print(f"Columns found: {df.columns.tolist()}")
except FileNotFoundError:
	print("FileNotFoundError: No such file or directory: 'monitor_actuals.csv'")
	exit()


client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, "PythonSimulator")

try:
	client.connect(broker_address, port)
	print(f"Connected to MQTT Broker: {broker_address}")
except Exception as e:
	print(f"Connection Failed: {e}")
	exit()

print("Start sending data...")
for index, row in df.iterrows():

	timestamp_val = row.get('timestamp') or row.get('Datetime')

	value_val = row.get('PJME_MW') or row.get('value') or 0

	payload = {
		"timestamp": str(timestamp_val),
		"value": float(value_val),  # ส่งค่าจริง
		"buildingId": "MFEC Tower"
	}

	payload_str = json.dumps(payload)
	client.publish(topic, payload_str)
	print(f"Sent: {payload_str}")

	time.sleep(10)

client.disconnect()
