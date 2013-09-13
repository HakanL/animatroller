#!/usr/bin/env python3
"""
Test of Python3
"""


#Import Modules
import os, sys
import pygame
from pygame.locals import *
import socket

if not pygame.mixer: print ('Warning, sound disabled')


soundFXdict = {}


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

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(('', 10009))

#    bg_sound = load_sound('myFile.wav')

    print ('Ready to receive on port 10009')

    try:
        while 1:
            data, addr = sock.recvfrom(1024)
    
            print ('Received: ', data)
    
            if data.startswith(b'!AUD:0,'):
                parts = data.decode('utf-8')[7:].strip().split(',')
                
                print ('parts: ', parts)
    
                if len(parts) < 2:
                    continue
                
                if parts[0] == 'FX':
                    fx_sound = load_fx(parts[1] + '.wav')
                    if fx_sound == None:
                        continue
                    fx_sound.stop()
                    fx_sound.play()


    except KeyboardInterrupt:
        print ('Aborting')
        pass
    finally:
        sock.close()
    
    print ('Done')


#this calls the 'main' function when this script is executed
if __name__ == '__main__':
    main()
    
                                  
