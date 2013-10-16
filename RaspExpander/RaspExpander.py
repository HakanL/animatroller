#!/usr/bin/env python3
"""
Raspberry Pi Animatroller Expander
"""


#Import Modules
import os, sys, argparse, math, time, threading, random
import pygame
from pygame.locals import *

from pythonosc import dispatcher
from pythonosc import osc_server
from pythonosc import osc_message_builder
from pythonosc import udp_client
from os import listdir
from os.path import isfile, join
import pifacecommon
import pifacedigitalio as pif
from serial import Serial
from serial import serialutil

if not pygame.mixer: print ('Warning, sound disabled')

ser = None
        

pfd = None
try:
    pif.init()
    pfd = pif.PiFaceDigital()
except pif.NoPiFaceDigitalDetectedError:
    print ('No PiFace card detected')
    pass

soundFXdict = {}
client = udp_client
last_fx_chn = None
last_fx_snd = None
bg_volume = 0.5
bg_files = []
last_input_values = [0] * 8


#functions to create our resources
def load_fx(name):

    sound = soundFXdict.get(name.lower())

    if sound is not None:
        return sound

    fullname = os.path.join('halloweensounds/fx', name)
    try:
        print ('Loading ', fullname)
        sound = pygame.mixer.Sound(fullname)
    except:
        pass
        print('Cannot load sound: ', name)
        return None

    soundFXdict[name.lower()] = sound
    return sound


def play_next_bg_track():
    print ('Next background track')
    
    index = random.randint(0, len(bg_files) - 1)
    print ('File =', bg_files[index])

    pygame.mixer.music.load(os.path.join('halloweensounds/bg', bg_files[index]))
    pygame.mixer.music.set_volume(bg_volume)
    pygame.mixer.music.play()


def decode_motor_command(cmd):
    print('Decode motor command:', cmd)
    cmds = cmd.split(',')

    motor_chn = 0
    motor_pos = None
    
    if len(cmds) >= 2:
        motor_chn = int(cmds[0])
        
        if cmds[1] == 'X':
            print('Motor', motor_chn, 'failed!')
            motor_pos = 'FAIL'
            
        elif cmds[1].startswith('S'):
            motor_pos = cmds[1]
            pos = int(cmds[1][1:])
            print('Motor', motor_chn, 'start moving, currently in position', pos)

        elif cmds[1].startswith('E'):
            motor_pos = cmds[1]
            pos = int(cmds[1][1:])
            print('Motor', motor_chn, 'done moving, currently in position', pos)
            
        else:
            pos = int(cmds[1])
            print('Motor', motor_chn, 'moving, currently in position', pos)

    if motor_pos is not None:
        motormsg = osc_message_builder.OscMessageBuilder(address = "/motor/feedback")
        motormsg.add_arg(motor_chn)
        motormsg.add_arg(motor_pos)
        motormsg = motormsg.build()
        client.send(motormsg)


def main():
    global bg_files
    """this function is called when the program starts.
    it initializes everything it needs, then runs in
    a loop until the function returns."""

    #Initialize Everything
    os.environ["SDL_VIDEODRIVER"] = "dummy"
    pygame.mixer.pre_init(frequency=44100, size=-16, channels=2, buffer=2048)
    pygame.init()
    screen = pygame.display.set_mode((80, 25))
    random.seed()

    pygame.mixer.music.set_endevent(pygame.constants.USEREVENT)

    # Find all background tracks
    bg_files = [ f for f in listdir('halloweensounds/bg') if isfile(join('halloweensounds/bg', f)) ]

    print('BG files =', len(bg_files))

    initmsg = osc_message_builder.OscMessageBuilder(address = "/init")
    initmsg = initmsg.build()
    client.send(initmsg)

    pfd_listener = None
    if pfd is not None:
        #pif.digital_write_pullup(6, 1)
        pfd_listener = pif.InputEventListener()
        for i in range(8):
            pif.digital_write_pullup(i, pif.INPUT_PULLUP)
            pfd_listener.register(i, pif.IODIR_ON, input_callback)
            pfd_listener.register(i, pif.IODIR_OFF, input_callback)
        pfd_listener.activate()

    print('Ready!')

    # auto-start BG music
    #play_next_bg_track()

    running = 1

    try:
        if ser is not None:
            ser.write("!!\r".encode('utf-8'))

        while running:
            for event in pygame.event.get(): # User did something
                if event.type == pygame.QUIT: # If user clicked close
                    running = 0
                if event.type == pygame.constants.USEREVENT:
                    # This event is triggered when the song stops playing
                    print ('Music ended')
                    play_next_bg_track()

            if ser is not None:
                serline = ser.readline(timeout=0.3)
                if serline != '' and serline.startswith('!IOX:0,'):
                    serline = serline[7:]
                    if len(serline) < 1:
                        continue
                    if serline[0] == '#':
                        print ('Serial: ACK')
                    elif serline[0:2] == 'M,':
                        decode_motor_command(serline[2:].rstrip())
                    else:
                        print ('Serial data:', serline)
                else:
                    if serline != '':
                        print(serline)
            else:
                time.sleep(0.1)

    except KeyboardInterrupt:
        print ('Aborting')
        pass

    if pfd is not None:
        pfd_listener.deactivate()
        
    print ('Done')


def send_input_msg(channel, button_value):
    print ('Input value', button_value, 'on channel', channel)
    buttonmsg = osc_message_builder.OscMessageBuilder(address = "/input")
    buttonmsg.add_arg(channel)
    buttonmsg.add_arg(button_value)
    buttonmsg = buttonmsg.build()
    client.send(buttonmsg)


def input_callback(event):
    global last_input_values

    print ('Input_callback', event.pin_num, 'direction', event.direction)
    
    if last_input_values[event.pin_num] != 1 - event.direction:
        send_input_msg(event.pin_num, 1 - event.direction)
        last_input_values[event.pin_num] = 1 - event.direction
        time.sleep(0.1)


def osc_init(args = None):
    print ('Animatroller running')


def osc_motor(chn, target, speed, timeout):
    print('Motor command: chn:', chn, '  target:', target, '  speed:', speed, '  timeout:', timeout)
    output = '!M,{0},{1},{2},{3}\r'.format(chn, target, speed, timeout)

    print('Output:', output)
    ser.write(output.encode('utf-8'))


def osc_playFx(file, leftvol = -1, rightvol = -1):
    global last_fx_snd, last_fx_chn

    print ('Play FX', file)
    fx_sound = load_fx(file + '.wav')
    if fx_sound is not None:
        last_fx_snd = fx_sound
        if last_fx_chn is not None:
            last_fx_chn.stop()
        last_fx_chn = fx_sound.play()
        
        leftvol = float(leftvol)
        rightvol = float(rightvol)
        
        if rightvol >= 0 and leftvol >= 0:
            last_fx_chn.set_volume(leftvol, rightvol)
        elif leftvol >= 0:
            last_fx_chn.set_volume(leftvol)


def osc_playNewFx(file, leftvol = -1, rightvol = -1):
    global last_fx_snd, last_fx_chn

    print ('Play New FX', file)
    fx_sound = load_fx(file + '.wav')
    if fx_sound is not None:
        last_fx_snd = fx_sound
        last_fx_chn = fx_sound.play()
        
        leftvol = float(leftvol)
        rightvol = float(rightvol)
        
        if rightvol >= 0 and leftvol >= 0:
            last_fx_chn.set_volume(leftvol, rightvol)
        elif leftvol >= 0:
            last_fx_chn.set_volume(leftvol)


def osc_cueFx(args):
    global last_fx_snd, last_fx_chn

    print ('Cue FX', args)
    fx_sound = load_fx(args + '.wav')
    if fx_sound is not None:
        last_fx_snd = fx_sound
        fx_sound.stop()
        last_fx_chn = None


def osc_pauseFx():
    print ('Pause FX')
    if last_fx_chn is not None:
        last_fx_chn.pause()


def osc_resumeFx():
    global last_fx_chn
    print ('Resume FX')
    if last_fx_chn is not None:
        last_fx_chn.unpause()
    elif last_fx_snd is not None:
        last_fx_chn = last_fx_snd.play()
        
def osc_bgVolume(volume):
    global bg_volume
    print ('Background volume', volume)
    bg_volume = float(volume)
    pygame.mixer.music.set_volume(bg_volume)


def osc_bgPlay():
    if pygame.mixer.music.get_busy():
        print ('Background resume')
        pygame.mixer.music.unpause()
    else:
        print ('Background play')
        play_next_bg_track()


def osc_bgPause():
    print ('Background pause')
    pygame.mixer.music.pause()


def osc_bgNext():
    print ('Background next')
    play_next_bg_track()


def osc_output(channel, value):
    if pfd is not None:
        print ('Output', channel, 'set to', value)
        pfd.output_pins[channel].value = value
    else:
        print ('No PiFace card')


class EnhancedSerial(Serial):
    def __init__(self, *args, **kwargs):
        #ensure that a reasonable timeout is set
        timeout = kwargs.get('timeout',0.1)
        if timeout < 0.01: timeout = 0.1
        kwargs['timeout'] = timeout
        Serial.__init__(self, *args, **kwargs)
        self.buf = ''
        
    def readline(self, maxsize=None, timeout=1):
        """maxsize is ignored, timeout in seconds is the max time that is way for a complete line"""
        tries = 0
        while 1:
            self.buf += self.read(512).decode('utf-8') 
            pos = self.buf.find('\r')
            if pos >= 0:
                line, self.buf = self.buf[:pos+1], self.buf[pos+1:]
                return line
            tries += 1
            if tries * self.timeout > timeout:
                break
        line, self.buf = self.buf, ''
        return line

    def readlines(self, sizehint=None, timeout=1):
        """read all lines that are available. abort after timout
        when no more data arrives."""
        lines = []
        while 1:
            line = self.readline(timeout=timeout)
            if line:
                lines.append(line)
            if not line or line[-1:] != '\r':
                break
        return lines


#this calls the 'main' function when this script is executed
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument("--ip",
        default="0.0.0.0", help="The ip to listen on")
    parser.add_argument("--port",
        type=int, default=5005, help="The port to listen on")
    parser.add_argument("--serverip",
        default="127.0.0.1", help="The server ip to send messages to")
    parser.add_argument("--serverport",
        type=int, default=3333, help="The server port to send messages to")
    parser.add_argument("--serialport", default="", help="Serial port to connect to")
    args = parser.parse_args()

    dispatcher = dispatcher.Dispatcher()
    dispatcher.map("/init", osc_init)
    dispatcher.map("/audio/fx/play", osc_playFx)
    dispatcher.map("/audio/fx/playnew", osc_playNewFx)
    dispatcher.map("/audio/fx/cue", osc_cueFx)
    dispatcher.map("/audio/fx/pause", osc_pauseFx)
    dispatcher.map("/audio/fx/resume", osc_resumeFx)
    dispatcher.map("/audio/bg/volume", osc_bgVolume)
    dispatcher.map("/audio/bg/play", osc_bgPlay)
    dispatcher.map("/audio/bg/pause", osc_bgPause)
    dispatcher.map("/audio/bg/next", osc_bgNext)
    dispatcher.map("/output", osc_output)
    dispatcher.map("/motor/exec", osc_motor)

    if args.serialport != "":
        sys.stdout.write("Serial port /dev/" + args.serialport + "\n")
        ser = EnhancedSerial("/dev/" + args.serialport, 38400, timeout=0.5)

    server = osc_server.ThreadingOSCUDPServer(
        (args.ip, args.port), dispatcher)
    print("Serving on {}".format(server.server_address))
    server_thread = threading.Thread(target=server.serve_forever)
    server_thread.start()

    client = udp_client.UDPClient(args.serverip, args.serverport)

    main()  
    pif.deinit()
    pygame.quit()
    server.shutdown()
    if ser is not None:
        ser.close()
    print ('Goodbye')
