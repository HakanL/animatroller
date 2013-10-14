CON
        _clkmode = xtal1 + pll16x                                               'Standard clock mode * crystal frequency = 80 MHz
        _xinfreq = 5_000_000
         
        MAXSTR_LENGTH = 63

VAR
  long  lock
  byte  str_buffer[MAXSTR_LENGTH+1]                     'String buffer for numerical strings
  long Pos[3]                            'Create buffer for two encoders (plus room for delta position support of 1st encoder)
                       
OBJ
  pst : "Parallax Serial Terminal"
  Encoder : "Quadrature Encoder"

  
PUB Main
  lock := locknew
  Encoder.Start(12, 1, 1, @Pos)           'Start continuous two-encoder reader (encoders connected to pins 8 - 11)
  
  pst.StartRxTx(31, 30, 0, 38_400)

  repeat
    seroutDec(string("M,1,"), Pos[0])            


PRI seroutDec(addr, val)
  repeat while lockset(lock)
  pst.str(string("!IOX:0,"))
  pst.str(addr)
  pst.dec(val)
  pst.char(13)
  lockclr(lock)
                                       