CON
        _clkmode = xtal1 + pll16x                                               'Standard clock mode * crystal frequency = 80 MHz
        _xinfreq = 5_000_000
        BTN1 = 8
        BTN2 = 9
        BTN3 = 10
        OUT1 = 20
        OUT2 = 21
        OUT3 = 22
        OUT4 = 23
        GECE_PIN = 24
         
        MAXSTR_LENGTH = 63

VAR
  long random
  byte value
  long cmdCounter

  long  inpStack[200]                                    ' local stack
  long  inpStack2[200]                                    ' local stack
  BYTE  cogMotor
  long  lock
  byte  str_buffer[MAXSTR_LENGTH+1]                     'String buffer for numerical strings
  byte  lastReadByte
  long Pos[3]                            'Create buffer for two encoders (plus room for delta position support of 1st encoder)
  long MotorTarget
  long MotorSpeed
  long MotorTimeout
  byte MotorFailed
  byte MotorTrigger
  byte PixelR
  byte PixelG
  byte PixelB
  long NumPixels
  
                       
OBJ
  DMXout : "dmxout"
  pst : "Parallax Serial Terminal"
  pwm  :  "PWMMotorDriver"
  Encoder : "Quadrature Encoder"
  timerMotor : "Timer32"
'  gece : "GECE"

  
PUB Main | cmd, chn, level
  lock := locknew
  
  pst.StartRxTx(31, 30, 0, 38_400)

'  gece.Start(GECE_PIN)
'  gece.set_standard_enum
  
  serout(string("X"))

  DMXout.Start(16)
  dira[BTN1]~               ' Set pin to input
  dira[BTN2]~               ' Set pin to input
  dira[BTN3]~               ' Set pin to input
  dira[OUT1]~~               ' Set pin to output
  dira[OUT2]~~               ' Set pin to output
  dira[OUT3]~~               ' Set pin to output
  dira[OUT4]~~               ' Set pin to output

'  cognew(InputReader, @inpStack)
  cognew(MotorController, @inpStack2)

'  gece.set_bulb(0, $CC, $FFF)
'  gece.set_bulb(1, $CC, $DDD)
'  gece.set_bulb2(2, 255, 255, 255)

  repeat   
    repeat
     
      if pst.CharIn <> "!"
        quit

      cmd := pst.CharIn
      cmdCounter := cmdCounter + 1
       
      if cmd == "!"
        ack
      elseif cmd == "L"
        chn := rxDec
        level := rxDec
        DMXout.Write(chn, level)
                                  
      elseif cmd == "O"
        chn := rxDec
        level := rxDec
        if chn == 1
          outa[OUT1] := level & 1
        elseif chn == 2
          outa[OUT2] := level & 1
        elseif chn == 3
          outa[OUT3] := level & 1
        elseif chn == 4
          outa[OUT4] := level & 1

      elseif cmd == "M"
        chn := rxDec
        MotorTarget := rxDec           
        MotorSpeed := rxDec
        MotorTimeout := rxDec
        MotorTrigger := 1           
        if MotorFailed
          serout(string("M,1,X"))
        else
          seroutDec(string("M,1,S"), Pos[0])            
           
'      elseif cmd == "P"
'        chn := rxDec
'        PixelR := rxDec
'        PixelG := rxDec
'        PixelB := rxDec
'        gece.set_bulb2(chn, PixelR, PixelG, PixelB) 

'      elseif cmd == "R"
'        ' Optimized pixel
'        chn := pst.CharIn
''        seroutDec(string("X,C="), chn)
''        NumPixels := 0            
'        repeat
''          NumPixels := NumPixels + 1
'          PixelR := pst.CharIn
'          PixelG := pst.CharIn
'          PixelB := pst.CharIn
'
''          seroutDec(string("X,R="), PixelR)            
''          seroutDec(string("X,G="), PixelG)            
''          seroutDec(string("X,B="), PixelB)            
'
'          gece.set_bulb2(chn, PixelR, PixelG, PixelB)
'          lastReadByte := pst.CharIn
'          chn := chn + 1
'          if lastReadByte == 13
''            seroutDec(string("X,Tot="), NumPixels)        
'            quit
           
      quit

    if lastReadByte == 13
      next       
    repeat until pst.CharIn == 13
  


PRI ReadButtons : bitmask
  bitmask := (ina[BTN3] * 4) + (ina[BTN2] * 2) + ina[BTN1] 

PRI InputReader | lastValue, lastButton1, lastButton2, lastButton3
  repeat
    lastButton1 := ina[BTN1]
    lastButton2 := ina[BTN2]
    lastButton3 := ina[BTN3]
    
    lastValue := ReadButtons
    repeat while lastValue == ReadButtons
    waitcnt(clkfreq / 20 + cnt)                                'Wait 0.05 seconds
    if lastValue <> ReadButtons
      if lastButton1 <> ina[BTN1]
        if ina[BTN1]      
          serout(string("I,1,1"))
        else
          serout(string("I,1,0"))
      if lastButton2 <> ina[BTN2]
        if ina[BTN2]      
          serout(string("I,2,1"))
        else
          serout(string("I,2,0"))     
      if lastButton3 <> ina[BTN3]
        if ina[BTN3]      
          serout(string("I,3,1"))
        else
          serout(string("I,3,0"))     



PRI MotorController | x
  pwm.Start(2, 1, 0, 15000)
  Encoder.Start(12, 1, 1, @Pos)           'Start continuous two-encoder reader (encoders connected to pins 8 - 11)
  timerMotor.Init

  repeat
    repeat until MotorTrigger <> 0 and MotorSpeed > 0 and ||(MotorTarget - Pos[0]) > 10

    if MotorTarget - Pos[0] > 0
      repeat x from 0 to MotorSpeed ' linearly advance speed from stopped to maximum speed forward
        setDutyAndWait(x)
        
      MoveMotorUntilInRange        
      pwm.Coast
       
    else
      repeat x from 0 to -MotorSpeed ' linearly advance speed from stopped to maximum speed forward
        setDutyAndWait(x)

      MoveMotorUntilInRange
      pwm.Halt        
         
    MotorSpeed := 0
    MotorTrigger := 0
     
    if MotorFailed <> 0
      serout(string("M,1,X"))
      quit
    else            
      seroutDec(string("M,1,E"), Pos[0])            

PRI MoveMotorUntilInRange | x
  timerMotor.Mark
  repeat until timerMotor.TimeOutS(MotorTimeout) or ||(MotorTarget - Pos[0]) =< 10
    timerMotor.Tick
'    seroutDec(string("M,1,"), Pos[0])
      
  if timerMotor.TimeOutS(MotorTimeout)
    MotorFailed := 1
    pwm.Halt
  else
    repeat x from pwm.GetDuty to 0 ' slowly stop motor
      setDutyAndWait(x)
      if ||(MotorTarget - Pos[0]) =< 1
        quit
   
PRI setDutyAndWait(duty)
  pwm.SetDuty(duty)
  waitcnt(clkfreq/200+cnt)




PUB rxDec : outvalue | place, ptr, x
{{
   Accepts and returns serial decimal values, such as "1234" as a number.
   String must end in a carriage return (ASCII 13) or comma
   x:= Serial.rxDec     ' accept string of digits for value
}}   
    place := 1                                           
    ptr := 0
    outvalue := 0                                             
    str_buffer[ptr] := pst.CharIn
    if str_buffer[0] == ","
      str_buffer[ptr] := pst.CharIn       
    ptr++
    repeat while (str_buffer[ptr-1] <> 13) and (str_buffer[ptr-1] <> ",")                     
       str_buffer[ptr] := pst.CharIn
       lastReadByte := str_buffer[ptr]                             
       ptr++
    if ptr > 2 
      repeat x from (ptr-2) to 1                            
        if (str_buffer[x] => ("0")) and (str_buffer[x] =< ("9"))
          outvalue := outvalue + ((str_buffer[x]-"0") * place)       
          place := place * 10                               
    if (str_buffer[0] => ("0")) and (str_buffer[0] =< ("9")) 
      outvalue := outvalue + (str_buffer[0]-48) * place
    elseif str_buffer[0] == "-"                                  
         outvalue := outvalue * -1
    elseif str_buffer[0] == "+"                               
         outvalue := outvalue
    
    
PRI ack
'  serout(string("#"))
  repeat while lockset(lock)
  pst.str(string("!IOX:0,"))
  pst.char("#")
  pst.char(13)
  lockclr(lock)

PRI serout(addr)
  repeat while lockset(lock)
  pst.str(string("!IOX:0,"))
  pst.str(addr)
  pst.char(13)
  lockclr(lock)

PRI seroutDec(addr, val)
  repeat while lockset(lock)
  pst.str(string("!IOX:0,"))
  pst.str(addr)
  pst.dec(val)
  pst.char(13)
  lockclr(lock)
                                       