import struct
from time import sleep
import bluetooth
from numpy import average
import sys
import pygame
import numpy
import csv
import matplotlib.pyplot as plt
import pandas as pd
frequency = input("frequency step: ")
x = int(input("max frequency: "))


f = open('output.csv', 'w')

header = ["frequency", "amplitude"]
writer = csv.writer(f)
writer.writerow(header)

sampleRate = 44100

pygame.mixer.init(44100,-16,2,512)


server_sock = bluetooth.BluetoothSocket(bluetooth.RFCOMM)
server_sock.bind(("", bluetooth.PORT_ANY))
server_sock.listen(1)

port = server_sock.getsockname()[1]

uuid = "94f39d29-7d6d-437d-973b-fba39e49d4ee"

bluetooth.advertise_service(server_sock, "SampleServer", service_id=uuid,
                            service_classes=[uuid, bluetooth.SERIAL_PORT_CLASS],
                            profiles=[bluetooth.SERIAL_PORT_PROFILE],
                            # protocols=[socket.OBEX_UUID]
                            )

print("connect to pc in mobile app")

client_sock, client_info = server_sock.accept()
print("Accepted connection from", client_info)
data = []
while not data:
    data = client_sock.recv(8)
input("Start with enter")
try:
    for i in range(x/frequency):
        arr = numpy.array([4096 * numpy.sin(2.0 * numpy.pi * frequency * x / sampleRate) for x in range(0, sampleRate)]).astype(numpy.int16)
        arr2 = numpy.c_[arr,arr]
        sound = pygame.sndarray.make_sound(arr2)
        sound.play(-1)

        client_sock.send(0b01.to_bytes(1, sys.byteorder))

        sleep(2)
        client_sock.send(0b10.to_bytes(1, sys.byteorder))

        data = client_sock.recv(8)

        averageAmplitude = struct.unpack("d", data)[0]
        print("Received", averageAmplitude)
        row = [str(frequency), str(averageAmplitude)]
        writer.writerow(row)

        
        
        frequency += 100
        sleep(1)
        sound.stop()
except OSError:
    pass

print("Disconnected.")

client_sock.close()
server_sock.close()
f.close() 
print("All done.")

plt.rcParams["figure.figsize"] = [7.00, 3.50]
plt.rcParams["figure.autolayout"] = True
columns = ["frequency", "amplitude"]
df = pd.read_csv("output.csv", usecols=columns)
print("Contents in csv file:", df)
plt.plot(df.frequency, df.amplitude)
plt.show()