#include "FastLED.h"

#define NUM_LEDS 60
#define DATA_PIN 5

CRGB leds[NUM_LEDS];

void setup() {
  Serial.begin(230400);

  FastLED.addLeds<WS2811, DATA_PIN, RGB>(leds, NUM_LEDS);
}

int serialGlediator () {
  while (!Serial.available()) {}
  return Serial.read();
}

void loop() {
  while (serialGlediator () != 1) {}

  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i].r = serialGlediator ();
    leds[i].g = serialGlediator ();
    leds[i].b = serialGlediator ();
  }
  
  FastLED.show();
}

