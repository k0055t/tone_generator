import numpy
import pygame

sampleRate = 44100

pygame.mixer.init(44100,-16,2,512)

frequency = 20
for i in range(41):
    arr = numpy.array([4096 * numpy.sin(2.0 * numpy.pi * frequency * x / sampleRate) for x in range(0, sampleRate)]).astype(numpy.int16)
    arr2 = numpy.c_[arr,arr]
    sound = pygame.sndarray.make_sound(arr2)
    sound.play(1)
    pygame.time.delay(1000)
    sound.stop()
    frequency *= 1.2