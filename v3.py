import bluetooth
import struct
import binascii

class GLMxxC(object):
    device_name = ''
    socket = None
    port = 0x0005  # depends on model type
    bluetooth_address = None
    connected = False
    cmds = {
            'measure':          b'\xC0\x40\x00\xEE',
            'laser_on':         b'\xC0\x41\x00\x96',
            'laser_off':        b'\xC0\x42\x00\x1E',
            'backlight_on':     b'\xC0\x47\x00\x20',
            'backlight_off':    b'\xC0\x48\x00\x62'
        }
    status = {
            0:  'ok',
            1:  'communication timeout',
            3:  'checksum error',
            4:  'unknown command',
            5:  'invalid access level',
            8:  'hardware error',
            10: 'device not ready',
        }

    def __init__(self, bluetooth_address=None):
        if bluetooth_address is None:
            self.find_GLMxxC()
        else:
            self.bluetooth_address = bluetooth_address
        self.connect()

    def connect(self):
            try:
                self.socket = bluetooth.BluetoothSocket(bluetooth.RFCOMM)
                self.socket.connect((self.bluetooth_address, self.port))
                self.connected = True
            except:
                self.socket.close()
                self.conencted = False
                
    def measure(self):
        self.socket.send(self.cmds['measure'])
        data = self.socket.recv(1024)
        print('received:', int(binascii.hexlify((data[0]))))
        if self.status[int(binascii.hexlify((data[0])))] is 'ok':
            try:
                        # distance to object from top of device
                distance = int(struct.unpack("<L", data[2:6])[0])*0.05
                return distance
            except:
                    return -1
        else:
            return -1