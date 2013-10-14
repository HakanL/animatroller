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

if not pygame.mixer: print ('Warning, sound disabled')

pif.init()
pfd = pif.PiFaceDigital()


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

    if sound != None:
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

    pif.digital_write_pullup(6, 1)
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
        while running:
            for event in pygame.event.get(): # User did something
                if event.type == pygame.QUIT: # If user clicked close
                    running = 0
                if event.type == pygame.constants.USEREVENT:
                    # This event is triggered when the song stops playing
                    print ('Music ended')
                    play_next_bg_track()

            time.sleep(0.1)

    except KeyboardInterrupt:
        print ('Aborting')
        pass

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


def osc_playFx(file, leftvol = -1, rightvol = -1):
    global last_fx_snd, last_fx_chn

    print ('Play FX', file)
    fx_sound = load_fx(file + '.wav')
    if fx_sound != None:
        last_fx_snd = fx_sound
        if last_fx_chn != None:
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
    if fx_sound != None:
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
    if fx_sound != None:
        last_fx_snd = fx_sound
        fx_sound.stop()
        last_fx_chn = None


def osc_pauseFx():
    print ('Pause FX')
    if last_fx_chn != None:
        last_fx_chn.pause()


def osc_resumeFx():
    global last_fx_chn
    print ('Resume FX')
    if last_fx_chn != None:
        last_fx_chn.unpause()
    elif last_fx_snd != None:
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
    print ('Output', channel, 'set to', value)
    pfd.output_pins[channel].value = value


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
    print ('Goodbye')
