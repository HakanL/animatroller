CON
        _clkmode = xtal1 + pll16x                                               'Standard clock mode * crystal frequency = 80 MHz
        _xinfreq = 5_000_000
         
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
  pst : "Parallax Serial Terminal"
  pwm  :  "PWMMotorDriver"
  timerMotor : "Timer32"

  
PUB Main
  lock := locknew
  
  pst.StartRxTx(31, 30, 0, 38_400)
  pwm.Start(2, 1, 0, 15000)
  timerMotor.Init

  serout(string("Init"))
  
  Wait(5)
  serout(string("FW"))

  pwm.SetDuty(100)
  Wait(2)
  pwm.Halt
  serout(string("Halt"))
  Wait(2)
  
  serout(string("BW"))
  pwm.SetDuty(-100)
  Wait(2)
  pwm.Halt
  serout(string("Halt"))
  Wait(2)

  pwm.Coast
  serout(string("Off"))


PRI wait(sec)
  timerMotor.Mark
  repeat until timerMotor.TimeOutS(sec)
    timerMotor.Tick



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
                                       