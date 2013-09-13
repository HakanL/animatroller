#!/usr/bin/env python3
"""
Raspberry Pi Animatroller Expander
"""


#Import Modules
import os, sys
import pygame
from pygame.locals import *

import argparse
import math
import time
import threading

from pythonosc import dispatcher
from pythonosc import osc_server
from pythonosc import osc_message_builder
from pythonosc import udp_client


if not pygame.mixer: print ('Warning, sound disabled')

soundFXdict = {}
client = udp_client
last_fx_chn = None
last_fx_snd = None



#functions to create our resources
def load_fx(name):

    sound = soundFXdict.get(name.lower())

    if sound != None:
        return sound

    fullname = os.path.join('fx', name)
    try:
        print ('Loading ', fullname)
        sound = pygame.mixer.Sound(fullname)
    except:
        pass
        print('Cannot load sound: ', name)
        return None

    soundFXdict[name.lower()] = sound
    return sound


def main():
    """this function is called when the program starts.
    it initializes everything it needs, then runs in
    a loop until the function returns."""

    #Initialize Everything
    os.environ["SDL_VIDEODRIVER"] = "dummy"
    pygame.mixer.pre_init(frequency=44100, size=-16, channels=2, buffer=1024)
    pygame.init()
    screen = pygame.display.set_mode((80, 25))

    running = 1
#    bg_sound = load_sound('myFile.wav')

    initmsg = osc_message_builder.OscMessageBuilder(address = "/init")
    initmsg = initmsg.build()
    client.send(initmsg)

    try:
        while running:
            time.sleep(0.2)

    except KeyboardInterrupt:
        print ('Aborting')
        pass

    print ('Done')


def osc_init(args = None):
    print ('Animatroller running')


def osc_playFx(args):
    global last_fx_snd, last_fx_chn

    print ('Play FX', args)
    fx_sound = load_fx(args + '.wav')
    if fx_sound != None:
        last_fx_snd = fx_sound
        if last_fx_chn != None:
            last_fx_chn.stop()
        last_fx_chn = fx_sound.play()


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
    dispatcher.map("/audio/fx/cue", osc_cueFx)
    dispatcher.map("/audio/fx/pause", osc_pauseFx)
    dispatcher.map("/audio/fx/resume", osc_resumeFx)

    server = osc_server.ThreadingOSCUDPServer(
        (args.ip, args.port), dispatcher)
    print("Serving on {}".format(server.server_address))
    server_thread = threading.Thread(target=server.serve_forever)
    server_thread.start()

    client = udp_client.UDPClient(args.serverip, args.serverport)

    main()
    server.shutdown()
    