#include <High_Temp.h>

#define SERIAL_BUF_MAX  64
char g_cSerialData[SERIAL_BUF_MAX + 1];

HighTemp ht1(A5, A4);
HighTemp ht2(A7, A6);

void setup() {
  Serial.begin(115200);
  Serial.flush();

  ht1.begin();
  ht2.begin();

  pinMode(2,OUTPUT);
  digitalWrite(2, LOW);
}

void loop() {
  if (Serial.available() > 0) {
    GetSerialData();
    if (strcmp(g_cSerialData, "GET_THMC") == 0) {
      Serial.print(ht1.getThmc());
      Serial.print(",");
      Serial.print(ht2.getThmc());
      Serial.println("");
    }
    else if (strcmp(g_cSerialData, "LASER_ON") == 0) {
      digitalWrite(2, HIGH);
    }
    else if (strcmp(g_cSerialData, "LASER_OFF") == 0) {
      digitalWrite(2, LOW);
    }
    else {
      Serial.print("NG\n");
    }
  }
}

// シリアルから文字列を取得する
void GetSerialData()
{
  int iCnt = 0;
  char c;

  while (1) {
    if (Serial.available() > 0) {
      c = Serial.read();
      if (c == '\n') {
        g_cSerialData[iCnt] = '\0';
        break;
      }
      g_cSerialData[iCnt++] = c;
    }
  }
}

