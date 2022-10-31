from cmath import log
from sqlite3 import Time
from pysinewave import SineWave
import time
import math
import bluetooth

nearby_devices = bluetooth.discover_devices(lookup_names=True)
print("Found {} devices.".format(len(nearby_devices)))

for addr, name in nearby_devices:
    print("  {} - {}".format(addr, name))

def toPitch(tone):
    result = math.log((tone / 440) ** 2, 2) + 9
    return result
sinewave = SineWave(-20, 1000)
for i in range(1, 100):
    j = i * 20
    sinewave.set_frequency(j)
    sinewave.play()
    time.sleep(2)
