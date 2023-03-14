#define PWM_PIN 9
#define SERIAL_BAUDRATE 9600

void setup() {
  Serial.begin(SERIAL_BAUDRATE);
  Serial.setTimeout(2000);
  pinMode(PWM_PIN, OUTPUT);
  pinMode(LED_BUILTIN, OUTPUT);
  
  // Set pin low to make sure vibration motor is off
  digitalWrite(PWM_PIN, LOW);
}

void loop() {
  if(!Serial.available())
    return;
  int value = Serial.parseInt();
  value = max(min(value, 254), 0);
  digitalWrite(LED_BUILTIN, value ? HIGH : LOW); 
  analogWrite(PWM_PIN, value);
}
