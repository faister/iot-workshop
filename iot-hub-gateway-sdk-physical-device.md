# How To Send Device-to-Cloud Messages with the Azure IoT Gateway SDK

This lab will show you how to use the [Azure IoT Gateway SDK][lnk-sdk] to send device-to-cloud telemetry to IoT Hub and route cloud-to-device commands from IoT Hub to a device. You will build and run an IoT Gateway with the Azure IoT Gateway SDK on a Raspberry Pi 3 running Raspbian. You will use a Texas Instruments SensorTag Bluetooth Low Energy (BLE) device to collect temperature data and send it to IoT Hub.

By the end of this lab, your IoT Gateway device will:

* Connect to a SensorTag device using the Bluetooth Low Energy (BLE) protocol.
* Connect to IoT Hub using the HTTPS protocol.
* Forward telemetry from the SensorTag device to IoT Hub.
* Route commands from IoT Hub to the SensorTag device.

## Architecture

> Note: You must read this section to fully understand how the Field Gateway works. Please do not skip this section.

The IoT Gateway contains the following modules:

* A **BLE Module** that interfaces with a BLE device to receive temperature data from the device and send commands to the device.
* A **BLE Cloud-to-Device Module** that translates the JSON messages coming from the cloud into BLE instructions for the *BLE module*.
* A **Logger Module** that logs all gateway messages to a local file.
* An **Identity Mapping Module** that translates between BLE device MAC addresses and Azure IoT Hub device identities.
* An **IoT Hub Module** that uploads telemetry data to an IoT hub and receives device commands from an IoT hub.
* A **BLE Printer Module** that interprets telemetry from the BLE device and prints formatted data to the console to enable troubleshooting and debugging.

### How Data Flows Through the Azure IoT Gateway SDK
The following block diagram illustrates the telemetry upload data flow pipeline:

![](media/iot-hub-gateway-sdk-physical-device/gateway_ble_upload_data_flow.png)

The steps that telemetry data takes when it travels from a BLE device to IoT Hub are:

1. The BLE device generates a temperature sample and sends it over Bluetooth to the BLE module in the gateway.
2. The BLE module receives the sample and publishes it to the broker along with the MAC address of the device.
3. The identity mapping module picks up this message and uses an internal table to translate the MAC address of the device into an IoT Hub device identity (a device ID and device key). It then publishes a new message that contains the temperature sample data, the MAC address of the device, the device ID, and the device key.
4. The IoT Hub module receives this new message and publishes it to IoT Hub.
5. The logger module logs all messages from the broker to a local file.

The following block diagram illustrates the device command data flow pipeline:

![](media/iot-hub-gateway-sdk-physical-device/gateway_ble_command_data_flow.png)

1. The IoT Hub module periodically polls the IoT hub for new command messages.
2. When the IoT Hub module receives a new command message, it publishes it to the broker.
3. The identity mapping module picks up the command message and uses an internal table to translate the IoT Hub device ID to a device MAC address. It then publishes a new message that includes the MAC address of the target device in the properties map of the message.
4. The BLE Cloud-to-Device module picks up this message and translates it into the proper BLE instruction for the BLE module. It then publishes a new message.
5. The BLE module picks up this message and executes the I/O instruction by communicating with the BLE device.
6. The logger module logs all messages from the broker to a disk file.

## Hardware Setup

> Note: Only follow these steps if you are using your own SD card. If you are using the SD card that is provided by this lab, you can go directly to the [Connect Azure IoT Gateway to SensorTag](#connect-azure-iot-gateway-to-sensortag) section.

### Prepare hardware
This lab assumes you are using a [Texas Instruments SensorTag](http://www.ti.com/ww/en/wireless_connectivity/sensortag2015/index.html) device connected to a Raspberry Pi 3 running Raspbian.

### Install Raspbian
You can use either of the following options to install Raspbian on your Raspberry Pi 3 device. 

* Use [NOOBS][lnk-noobs], a graphical user interface, to install the latest version of Raspbian. 
* Manually [download][lnk-raspbian] and write the latest image of the Raspbian operating system to a SD card. 

### Install BlueZ 5.37
The BLE modules talk to the Bluetooth hardware via the BlueZ stack. You need version 5.37 of BlueZ for the modules to work correctly. These instructions make sure the correct version of BlueZ is installed.

1. Stop the current bluetooth daemon:
   
    ```
    sudo systemctl stop bluetooth
    ```
2. Install the BlueZ dependencies. 
   
    ```
    sudo apt-get update
    sudo apt-get install bluetooth bluez-tools build-essential autoconf glib2.0 libglib2.0-dev libdbus-1-dev libudev-dev libical-dev libreadline-dev
    ```
3. Download the BlueZ source code from bluez.org. 
   
    ```
    wget http://www.kernel.org/pub/linux/bluetooth/bluez-5.37.tar.xz
    ```
4. Unzip the source code.
   
    ```
    tar -xvf bluez-5.37.tar.xz
    ```
5. Change directories to the newly created folder.
   
    ```
    cd bluez-5.37
    ```
6. Configure the BlueZ code to be built.
   
    ```
    ./configure --disable-udev --disable-systemd --enable-experimental
    ```
7. Build BlueZ.
   
    ```
    make
    ```
8. Install BlueZ once it is done building.
   
    ```
    sudo make install
    ```
9. Change systemd service configuration for bluetooth so it points to the new bluetooth daemon in the file `/lib/systemd/system/bluetooth.service`. Replace the 'ExecStart' line with the following text: 
    
    ```
    ExecStart=/usr/local/libexec/bluetooth/bluetoothd -E
    ```
## Connect Azure IoT Gateway to SensorTag
### Enable connectivity to the SensorTag device from your Raspberry Pi 3
Before running the sample, you need to verify that your Raspberry Pi 3 can connect to the SensorTag device.


1. Ensure the `rfkill` utility is installed.
   
    ```
    sudo apt-get install rfkill
    ```
2. Unblock bluetooth on the Raspberry Pi 3 and check that the version number is **5.37**.
   
    ```
    sudo rfkill unblock bluetooth
    bluetoothctl --version
    ```
3. Start the bluetooth service and execute the **bluetoothctl** command to enter an interactive bluetooth shell. 
   
    ```
    sudo systemctl start bluetooth
    bluetoothctl
    ```
4. Enter the command **power on** to power up the bluetooth controller. You should see output similar to:
   
    ```
    [NEW] Controller 98:4F:EE:04:1F:DF C3 raspberrypi [default]
    ```
5. While still in the interactive bluetooth shell, enter the command **scan on** to scan for bluetooth devices. You should see output similar to:
   
    ```
    Discovery started
    [CHG] Controller 98:4F:EE:04:1F:DF Discovering: yes
    ```
6. Make the SensorTag device discoverable by pressing the small button (the green LED should flash). The Raspberry Pi 3 should discover the SensorTag device:
   
    ```
    [NEW] Device A0:E6:F8:B5:F6:00 CC2650 SensorTag
    [CHG] Device A0:E6:F8:B5:F6:00 TxPower: 0
    [CHG] Device A0:E6:F8:B5:F6:00 RSSI: -43
    ```
   
    In this example, you can see that the MAC address of the SensorTag device is **A0:E6:F8:B5:F6:00**.
7. Turn off scanning by entering the **scan off** command.
   
    ```
    [CHG] Controller 98:4F:EE:04:1F:DF Discovering: no
    Discovery stopped
    ```
8. Connect to your SensorTag device using its MAC address by entering **connect \<MAC address>**. Note that the sample output below is abbreviated:
   
    ```
    Attempting to connect to A0:E6:F8:B5:F6:00
    [CHG] Device A0:E6:F8:B5:F6:00 Connected: yes
    Connection successful
    [CHG] Device A0:E6:F8:B5:F6:00 UUIDs: 00001800-0000-1000-8000-00805f9b34fb
    ...
    [NEW] Primary Service
            /org/bluez/hci0/dev_A0_E6_F8_B5_F6_00/service000c
            Device Information
    ...
    [CHG] Device A0:E6:F8:B5:F6:00 GattServices: /org/bluez/hci0/dev_A0_E6_F8_B5_F6_00/service000c
    ...
    [CHG] Device A0:E6:F8:B5:F6:00 Name: SensorTag 2.0
    [CHG] Device A0:E6:F8:B5:F6:00 Alias: SensorTag 2.0
    [CHG] Device A0:E6:F8:B5:F6:00 Modalias: bluetooth:v000Dp0000d0110
    ```
   
    > Note: You can list the GATT characteristics of the device again using the **list-attributes** command.
9. You can now disconnect from the device using the **disconnect** command and then exit from the bluetooth shell using the **quit** command:
   
    ```
    Attempting to disconnect from A0:E6:F8:B5:F6:00
    Successful disconnected
    [CHG] Device A0:E6:F8:B5:F6:00 Connected: no
    ```

You're now ready to run the BLE Gateway sample on your Raspberry Pi 3.

## Run the BLE Gateway Sample
To run the BLE sample, you need to complete three tasks:

* Configure two sample devices in your IoT Hub.
* Build the IoT Gateway SDK on your Raspberry Pi 3 device.
* Configure and run the BLE sample on your Raspberry Pi 3 device.

At the time of writing, the IoT Gateway SDK only supports gateways that use BLE modules on Linux.

### Configure Two Sample Devices in your IoT Hub
* [Create an IoT hub][lnk-create-hub] in your Azure subscription, you will need the name of your hub to complete this lab. If you don't have an account, you can ask a proctor to assign you one.
* Add one device called **SensorTag_01** to your IoT hub and make a note of its id and device key. You can use the [Device Explorer or iothub-explorer][lnk-explorer-tools] tools to add this device to the IoT hub you created in the previous step and to retrieve its key. You will map this device to the SensorTag device when you configure the gateway.

### Build the Azure IoT Gateway SDK on your Raspberry Pi 3

> Note: You can skip this step if you are using the provided SD card.

Install dependancies for the Azure IoT Gateway SDK.

``` 
sudo apt-get -y install curl build-essential libcurl4-openssl-dev git cmake libssl-dev uuid-dev valgrind libglib2.0-dev libtool autoconf

```
Use the following commands to clone the IoT Gateway SDK and all its submodules to your home directory:

```
cd ~
git clone --recursive https://github.com/Azure/azure-iot-gateway-sdk.git 
cd azure-iot-gateway-sdk
git submodule update --init --recursive
```

When you have a complete copy of the IoT Gateway SDK repository on your Raspberry Pi 3, you can build it using the following command from the folder that contains the SDK:

```
./tools/build.sh
```

### Configure and Run the BLE Sample on your Raspberry Pi 3
To bootstrap and run the sample, you need to configure each module that participates in the gateway. This configuration is provided in a JSON file and you need to configure all five participating modules. There is a sample JSON file provided in the repository called **gateway_sample.json** which you can use as the starting point for building your own configuration file. This file is in the **samples/ble_gateway/src** folder in local copy of the IoT Gateway SDK repository.

The following sections describe how to edit this configuration file for the BLE sample and assume that the IoT Gateway SDK repository is in the **/home/pi/code/azure-iot-gateway-sdk/** folder on your Raspberry Pi 3. If the repository is elsewhere, you should adjust the paths accordingly:

#### Logger Configuration
Assuming the gateway repository is located in the home folder, configure the logger module as follows:

```json
{
    "module name": "logger",
    "loading args": {
      "module path": "build/modules/logger/liblogger.so"
    },
    "args":
    {
        "filename":"gw_logger.log"
    }
}
```

#### BLE Module Configuration
The sample configuration for the BLE device assumes a Texas Instruments SensorTag device. Any standard BLE device that can operate as a GATT peripheral should work but you will need to update the GATT characteristic IDs and data (for write instructions). Add the MAC address of your SensorTag device: 

```json
{
  "module name": "SensorTag",
  "loading args": {
    "module path": "build/modules/ble/libble.so"
  },
  "args": {
    "controller_index": 0,
    "device_mac_address": "<<AA:BB:CC:DD:EE:FF>>",
    "instructions": [
      {
        "type": "read_once",
        "characteristic_uuid": "00002A24-0000-1000-8000-00805F9B34FB"
      },
      {
        "type": "read_once",
        "characteristic_uuid": "00002A25-0000-1000-8000-00805F9B34FB"
      },
      {
        "type": "read_once",
        "characteristic_uuid": "00002A26-0000-1000-8000-00805F9B34FB"
      },
      {
        "type": "read_once",
        "characteristic_uuid": "00002A27-0000-1000-8000-00805F9B34FB"
      },
      {
        "type": "read_once",
        "characteristic_uuid": "00002A28-0000-1000-8000-00805F9B34FB"
      },
      {
        "type": "read_once",
        "characteristic_uuid": "00002A29-0000-1000-8000-00805F9B34FB"
      },
      {
        "type": "write_at_init",
        "characteristic_uuid": "F000AA02-0451-4000-B000-000000000000",
        "data": "AQ=="
      },
      {
        "type": "read_periodic",
        "characteristic_uuid": "F000AA01-0451-4000-B000-000000000000",
        "interval_in_ms": 1000
      },
      {
        "type": "write_at_exit",
        "characteristic_uuid": "F000AA02-0451-4000-B000-000000000000",
        "data": "AA=="
      }
    ]
  }
}
```

#### IoT Hub Module
Add the name of your IoT Hub. The suffix value is typically **azure-devices.net**:

```json
{
  "module name": "IoTHub",
  "loading args": {
    "module path": "build/modules/iothub/libiothub.so"
  },
  "args": {
    "IoTHubName": "<<Azure IoT Hub Name>>",
    "IoTHubSuffix": "<<Azure IoT Hub Suffix>>",
    "Transport": "AMQP"
  }
}
```

#### Identity Mapping Module Configuration
Add the MAC address of your SensorTag device and the device Id and key of the **SensorTag_01** device you added to your IoT Hub:

```json
{
  "module name": "mapping",
  "loading args": {
    "module path": "build/modules/identitymap/libidentity_map.so"
  },
  "args": [
    {
      "macAddress": "<<AA:BB:CC:DD:EE:FF>>",
      "deviceId": "<<Azure IoT Hub Device ID>>",
      "deviceKey": "<<Azure IoT Hub Device Key>>"
    }
  ]
}
```

#### BLEPrinter Module Configuration
```json
{
    "module name": "BLEPrinter",
    "loading args": {
      "module path": "build/samples/ble_gateway/ble_printer/libble_printer.so"
    },
    "args": null
}
```

#### BLEC2D Module Configuration
```json
{
    "module name": "BLEC2D",
    "loading args": {
      "module path": "build/modules/ble/libble_c2d.so"
    },
    "args": null
}
```

#### Routing Configuration
The following configuration ensures the following:

* The **Logger** module receives and logs all messages.
* The **SensorTag** module sends messages to both the **mapping** and **

** modules.
* The **mapping** module sends messages to the **IoTHub** module to be sent up to your IoT Hub.
* The **IoTHub** module sends messages back to the **mapping** module.
* The **mapping** module sends messages back to the **SensorTag** module.

```json
"links" : [
    {"source" : "*", "sink" : "Logger" },
    {"source" : "SensorTag", "sink" : "mapping" },
    {"source" : "SensorTag", "sink" : "BLEPrinter" },
    {"source" : "mapping", "sink" : "IoTHub" },
    {"source" : "IoTHub", "sink" : "mapping" },
    {"source" : "mapping", "sink" : "BLEC2D" },
    {"source" : "BLEC2D", "sink" : "SensorTag"}
 ]
```

To run the sample, pass the path to the JSON configuration file to the **ble_gateway** binary. If you used the **gateway_sample.json** file, the command is below. Execute this command from the azure-iot-gateway-sdk directory

```
./build/samples/ble_gateway/ble_gateway ./samples/ble_gateway/src/gateway_sample.json
```

You may need to press the small button on the SensorTag device to make it discoverable before you run the sample.

When you run the sample, you can use the [Device Explorer or iothub-explorer][lnk-explorer-tools] tool to monitor the messages the gateway forwards from the SensorTag device.

## Send Cloud-to-Device Messages
The BLE module also supports sending instructions from Azure IoT Hub to the device. You can use the [Azure IoT Hub Device Explorer](https://github.com/Azure/azure-iot-sdks/blob/master/tools/DeviceExplorer/doc/how_to_use_device_explorer.md) or the [IoT Hub Explorer](https://github.com/Azure/azure-iot-sdks/tree/master/tools/iothub-explorer) to send JSON messages that the BLE gateway module passes on to the BLE device.
If you are using the Texas Instruments SensorTag device then you can turn on the red LED, green LED, or buzzer by sending commands from IoT Hub. To do this, first send the following two JSON messages in order. Then you can send any of the commands to turn on the lights or buzzer.

1 Reset all LEDs and the buzzer (turn them off)
  
    ```json
    {
      "type": "write_once",
      "characteristic_uuid": "F000AA65-0451-4000-B000-000000000000",
      "data": "AA=="
    }
    ```
2 Configure I/O as 'remote'
  
    ```json
    {
      "type": "write_once",
      "characteristic_uuid": "F000AA66-0451-4000-B000-000000000000",
      "data": "AQ=="
    }
    ```
* Turn on the red LED
  
    ```json
    {
      "type": "write_once",
      "characteristic_uuid": "F000AA65-0451-4000-B000-000000000000",
      "data": "AQ=="
    }
    ```
* Turn on the green LED
  
    ```json
    {
      "type": "write_once",
      "characteristic_uuid": "F000AA65-0451-4000-B000-000000000000",
      "data": "Ag=="
    }
    ```
* Turn on the buzzer
  
    ```json
    {
      "type": "write_once",
      "characteristic_uuid": "F000AA65-0451-4000-B000-000000000000",
      "data": "BA=="
    }
    ```

## Next Steps
If you want to gain a more advanced understanding of the IoT Gateway SDK and experiment with some code examples, visit the following developer tutorials and resources:

* [Azure IoT Gateway SDK][lnk-sdk]

To further explore the capabilities of IoT Hub, see:

* [Developer guide][lnk-devguide]

<!-- Links -->
[lnk-ble-samplecode]: https://github.com/Azure/azure-iot-gateway-sdk/blob/master/samples/ble_gateway_hl
[lnk-free-trial]: https://azure.microsoft.com/pricing/free-trial/
[lnk-explorer-tools]: https://github.com/Azure/azure-iot-sdks/blob/master/doc/manage_iot_hub.md
[lnk-sdk]: https://github.com/Azure/azure-iot-gateway-sdk/
[lnk-noobs]: https://www.raspberrypi.org/documentation/installation/noobs.md
[lnk-raspbian]: https://www.raspberrypi.org/downloads/raspbian/


[lnk-devguide]: iot-hub-devguide.md
[lnk-create-hub]: iot-hub-create-through-portal.md 
