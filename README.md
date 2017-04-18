# IoT Workshop 2017 - Australia 

Welcome to our IoT workshop! 

We are so glad to have you with us today. We put together this workshop to give you some hands-on experience with the Microsoft Azure IoT suite. Our goal is to get you up-to-speed on the latest developments so you can take the knowledge back to your office and start implementing IoT solutions with them.

## Prerequisites and Course Materials
This is a technical workshop and you're expected to be comfortable writing and understanding code (we will chiefly use Node.js throughout). Also, you're expected to be comfortable around Linux, be able to SSH in and know how to use text editors (such as **vi** or **nano**). Basic understanding of Azure is essential, but not critical.

Please make sure to complete the following prerequisites prior to coming to the workshop
- [Install](http://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html) PuTTY (Windows users only)
- [Download](https://github.com/Azure/azure-iot-sdk-csharp/releases) Azure IoT Device Explorer (Windows only) or [Install](https://github.com/Azure/azure-iot-sdks/blob/mvp_summit/doc/manage_iot_hub.md#iothub-explorer) a command line utility  
- [Activate](https://azure.microsoft.com/en-au/offers/ms-azr-0044p/) a free trial Azure subscription and familiarise yourself with the [Azure Portal](https://portal.azure.com/). Spin up a linux VM, SSH in, resize it and don't forget to tear it or shut it down. Explore how to use keyboard shortcuts - they save time!
- [Download](http://TBC) the slides for your reference

## Agenda
Day 1: 
- Azure IoT Suite: Level 300 intro 
- Environment setup: azure subscription, SSH access to your Raspberry PI, set up prerequisites
- Lab 1: Azure IoT Device Management

Day 2:
- Lab 2: Azure IoT Gateway SDK
- Bonus Challenges
- Wrap-up
- 🍕🍕🍕

## Getting Started
Each group will be provided with a Raspberry Pi 3 running Raspbian Jessie, a Texas Instruments(TI) 
Bluetooth Low Energy (BLE) Sensor Tag, and an Azure Subscription.

We are presenting two labs today, one for **Azure IoT Device Management** and 
one for the **Azure IoT Gateway SDK**.  The Azure IoT Device Management lab will introduce you to the concepts of Device Twins and Direct Methods. The Azure IoT Gateway SDK lab will introduce you to the open source SDK for building IoT Gateway devices enabling, for instance, non-internet connected device to send data to Azure IoT. 

> Proctors are available to help you work through these workshops, answer questions or talk tech.

Your Raspberry Pis comes with a stock standard version of Raspbian. Whilst you can skip the 'Installing Raspbian' sections in the labs, you will need to clone and compile the SDKs.

## Azure IoT Device Management Lab

This lab will demonstrate the **Device Twins** and **Direct Methods** features and is available [here](https://github.com/Azure/azure-iot-sdks/tree/mvp_summit/c/serializer/samples/devicetwin_configupdate#how-to-update-configuration-and-reboot-an-iot-device-with-azure-iot-device-twins). 

> Please make sure you set the correct timezone by running `sudo raspi-config`

> Please make sure you synchronise the time by running `sudo apt-get -y install ntp ntpdate ; sudo service ntp stop; sudo ntpdate 0.ubuntu.pool.ntp.org; sudo service ntp start`

> Your Raspberry Pis comes with a stock standard version of Raspbian, please skip over the instructions to install Raspbian in the lab

## Azure IoT Gateway SDK Lab 

This lab will demonstrate the open source **Azure IoT Gateway SDK** using a Bluetooth Low Energy (BLE) Sensor Tag, Raspberry Pi and Node.js.

The Azure IoT Gateway SDK lab is available [here](iot-hub-gateway-sdk-physical-device.md).


> Please read the architectural introduction before jumping to the "Enable connectivity to the Sensor Tag device from your Raspberry Pi 3 device"
section to configure your TI BLE Sensor Tag.

> Your Raspberry Pis comes with a stock standard version of Raspbian, please skip over the instructions to install Raspbian in the lab.

## Bonus Challenges

You are likely an overachiever, so we've included a few extra challenges!  Please make sure you complete the [Azure IoT Gateway SDK lab](iot-hub-gateway-sdk-physical-device.md) first.

> Note: Your Raspberry Pi has been setup with the required tooling 
to run the [Azure IoT Gateway SDK Examples in Node.js](https://github.com/Azure/azure-iot-gateway-sdk/blob/master/doc/nodejs_how_to.md#linux-1).

### Manually Batching Messages
- Create a Node.js Module that concatenates messages from the `node_sensor` using 
a `|` delimiter and then posts them to the Gateway's internal message bus. 

### Compress Batched Messages  
- Develop a Node.js Module that compresses the batched messages and posts them to 
the Gateway's internal message bus.

### Implement an IoT Hub Writer
- Copy and configure the IoT Writer Node.js from the Azure IoT Gateway SDK Sample Project [here](https://github.com/Azure/azure-iot-gateway-sdk/blob/master/samples/nodejs_simple_sample/nodejs_modules/iothub_writer.js).

### Create an Azure Function to Decompress & Shred Messages
- Wire up an Azure Function using your IoT Hub's Event Hub endpoint and utilize 
the IoT Samples -> DecompressShred -> NodeJs Azure Function to decompress and 
shred your IoT Hub messages, posting each individual message to an Event Hub for 
processing by Azure Stream Analytics.

### Create an Azure Stream Analytics Query
- Create an Azure Stream Analytics query that simply selects all the data from your 
Event Hub and outputs the results to Power BI, displaying aggregate metrics.

### Create a Power BI Dashboard
- Create a [Power BI](http://app.powerbi.com) Dashboard that visualizes your TI Sensor Tag data in creative ways.  Feel free to use any of the Power BI Custom Visuals available [here](https://store.office.com/en-us/appshome.aspx?productgroup=PowerBI). You can learn how to create Power BI Dashboards from a Stream Analytics Output [here](https://azure.microsoft.com/en-us/documentation/articles/stream-analytics-power-bi-dashboard/).
