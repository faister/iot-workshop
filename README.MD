# IoT Edge Hack Lab - Microsoft IoT Cloud Workshop - Australia 

Welcome to our IoT Cloud Workshop and choosing this hack lab. This content was originally developed along with Igor Izotov, a Technical Evangelist in Microsoft Australia.

We are so glad to have you with us today. We put together this workshop to give you some hands-on experience with the Microsoft Azure IoT Suite. Our goal is to get you up-to-speed on the latest developments so you can take the knowledge back to your office and start implementing IoT solutions with them.

>Please note that the contents of this workshop have been updated to reflect the [latest changes](https://github.com/Azure/iot-edge) to IoT Edge. The old content is still available in [/v1](./v1/README.md)

## Prerequisites
This is a technical workshop and you're expected to be comfortable writing and understanding code (we will chiefly use .NET Core). Also, you're expected to be comfortable around Linux, be able use SSH, bash, Visual Studio Code and know how to use text editors (such as **vi** or **nano**). Basic understanding of Docker Containers and Azure is essential, but not critical.

Please make sure to complete the following prerequisites prior to coming to the workshop
- [Install](http://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html) PuTTY (Windows users only) or [Install](https://docs.microsoft.com/en-us/windows/wsl/install-win10) the Windows Subsystem for Linux which gives you a nifty Bash shell.
- [Download](https://github.com/Azure/azure-iot-sdk-csharp/releases) Azure IoT Device Explorer (Windows only) or [Install](https://github.com/Azure/iothub-explorer) iothub-explorer, a command line utility  
- [Activate](https://azure.microsoft.com/en-au/free/) a free trial Azure subscription and familiarise yourself with the [Azure Portal](https://portal.azure.com/)
- [Install](https://code.visualstudio.com/docs/setup/setup-overview) Visual Studio Code
- [Install](https://www.microsoft.com/net/learn/get-started/windows#linuxredhat_2) .NET SDK on your machine
- [Register](https://hub.docker.com/) on Docker Hub
- [Install](https://www.docker.com/community-edition) Docker Community Edition

## Agenda
Day 1: 
- Azure IoT Suite: Level 300 intro 
- [Lab 0: Environment setup: Azure subscription, SSH access to your Raspberry PI, set up prerequisites](#lab0)
- [Lab 1: Deploy Simulated Device on IoT Edge](#lab-1-deploy-simulated-device-on-iot-edge)
- [Lab 2: Develop and deploy a custom C# module to connect to Sensor Tag  on IoT Edge](#lab-2-develop-and-deploy-a-custom-c-module-to-connect-to-sensor-tag--on-iot-edge)

Day 2:
- [Lab 3: Deploy Azure Stream Analytics on IoT Edge](#lab-3-deploy-azure-stream-analytics-on-iot-edge)
- Time for guided experimentation, try some of the [challenges](#additional-challenges)
- Wrap-up

## Getting Started
Each group will be provided with a Raspberry Pi 3 running [Raspbian Stretch with Desktop](https://www.raspberrypi.org/downloads/raspbian/), a Texas Instruments(TI) Bluetooth Low Energy (BLE) Sensor Tag.

## IMPORTANT NOTE
You may experience limited bandwidth at the workshop venue. We don't take such chances so that we have downloaded required packages and updated Raspbian with the latest packages and binaries. If you encounter any step that instructs you download a package, or to pull down a Docker image, you do not need to do so because it is already pre-loaded onto your Raspberry Pi. Even the IoT Edge module images are pre-loaded so that you don't have to wait to download this off a potentially slow network.

> Proctors are available to help you work through these workshops, answer questions or talk tech.

### What's in the Pre-baked Image
For your convenience we pre-baked an image that you can [download](https://scrapyard.blob.core.windows.net/share/2018-02-03-IoTCloudWorkshopRPi.7z) and try on your own Raspberry Pi. The image is compressed using [7-Zip](http://www.7-zip.org/) so extract the .img before flashing your micro SDHC, the requirement is at least 16GB. To flash the image, use [Win32 Disk Imager](https://sourceforge.net/projects/win32diskimager/files/latest/download). This image is based on stock standard [Raspbian Stretch with Desktop](https://www.raspberrypi.org/downloads/raspbian/) with the following tweaks:
- VNC server has been enabled, you can access you RPI's GUI using [VNC Viewer](https://www.realvnc.com/download/viewer/)
- Crontab [script](./src/aux-files/mailip) notifying proctors when raspberry PIs receive or refresh an IP address to simplify the classroom process. On your Raspberry Pi image, the script is located in /home/pi. Just modify the email address to your personal email.
- SSH enabled
- Swap increased to 1G

## Lab 0: Environment Setup
1. Make sure you've completed the [prerequisites](#prerequisites) before the workshop 
1. Make sure you can access [Azure Portal](https://portal.azure.com/) and have an active Azure Subscription
    > speak to one of the proctors if you need an Azure Subscription or [activate](https://azure.microsoft.com/en-au/free/) a free trial yourself
2. The procrotors will tell you the IP address of your Raspberry PI, make sure you can access it via SSH and VNC
    > username: **pi**, password: **raspberry**
3. Install [Docker](https://www.raspberrypi.org/blog/docker-comes-to-raspberry-pi/) on your Raspberry PI by running (already pre-installed)
    ```bash
    curl -sSL https://get.docker.com | sh
    ```
    * Check docker instalation by running

    ```bash
    sudo docker run hello-world
    ```
4. Install [bluepy](https://github.com/IanHarvey/bluepy) - a Python interface to Bluetooth LE on Linux (already pre-installed)
    ```bash
    sudo apt-get -y install python-pip libglib2.0-dev python-dev libffi-dev libssl-dev
    sudo pip install bluepy
    ```
5. Install [redis](https://redis.io/) - redis will be used as a pub/sub mechanism between the Sensor Tag (accessed via bluepy) and the IoT Edge (already pre-installed)
    ```bash
    sudo apt-get -y install redis-server
    sudo pip install redis
    ``` 
    * edit `/etc/redis/redis.conf`, commenting out the bind line on 127.0.0.1 to make Redis listen on all available ip addresses, adding a new line: `maxmemory 512mb`. Save the config file
    * Bounce Redis and enable the service to start automatically
        ```bash
        sudo service redis-server restart
        sudo update-rc.d redis-server enable
        ``` 
    * (bonus challenge) to understand more about Redis Pub/Sub, follow [this](https://www.toptal.com/go/going-real-time-with-redis-pubsub#peek-at-redis-pubsub) example 

6. Grab a python script that will interface with the Sensor Tag
    ```bash
    curl -sSL https://raw.githubusercontent.com/iizotov/iot-workshop/master/src/iotedgebleadapter/sensortag.py | tee ~pi/sensortag.py
    ```
    * Note the MAC address printed on the label of your Sensor Tag and launch the Python script by running
        ```bash
        python ~pi/sensortag.py --all -t 0.5 --redisip 127.0.0.1 --redischannel data <mac address>
        ```
    * If you do not know the MAC address of your Sensor Tag, run the following tool:
       ```
       bluetoothctl
       scan on
       ```
       > (briefly push the power button of your Sensor Tag to initiate the Bluetooth connection with your Raspberry Pi, you will see a device with Sensor Tag name to it, copy/record the MAC address)
       ```
       scan off
       quit
       ```
    * briefly push the power button on your Sensor Tag to initiate connection. Once established, you should see the telemetry being read from all sensors sequentially every 0.5 seconds and pushed into Redis running on `127.0.0.1`, channel `data`
    * (bonus challenge) to explore the json data pushed into the `data` channel in Redis, use `redis-cli` in a separate SSH session, issuing the following command: `SUBSCRIBE data`
    > If something's wrong, bounce bluetooth by running
`sudo /etc/init.d/bluetooth restart`

## Lab 1: Deploy Simulated Device on IoT Edge
1. Follow instructions [here](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-simulate-device-Linux) to:
    * Create an IoT hub
        > it is recommended to create an **S1** IoT Hub instance instead of an **F1 - Free** instance to avoid hittind the daily message quota
    * Register an IoT Edge device
    * Install and start the IoT Edge runtime
    * Deploy a simulated device to IoT Edge
2. Connect to the IoT Hub using either [Azure IoT Device Explorer](https://github.com/Azure/azure-iot-sdk-csharp/releases) or [iothub-explorer](https://github.com/Azure/iothub-explorer) and monitor the telemetry flowing through
3. Emulate a container crash by noting down the id of one of the docker  containers by killing it
    ```bash
    sudo docker kill tempSensor
    ```
    * observe how the IoT Edge Runtime heals itself by running 
    ```bash
    sudo watch docker ps
    ```
    * Observe what Docker images are pulled by iotedgectl by running
    ```
    sudo docker images
    ```
    
## Lab 2: Develop and deploy a custom C# module to connect to Sensor Tag  on IoT Edge
Follow the [instructions](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module) to create a custom C# module that will:
* Subscribe to Redis instance listening for telemetry from your Sensor Tag. The Redis channel and IP address will be specified vi the [Module Twin](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-modules#module-twins) capability
* Push the telemetry to the module output, to be further routed to the IoT Hub via the IoT Edge [routes](https://docs.microsoft.com/en-us/azure/iot-edge/module-composition#specify-the-routes) mechanism

> **TODO! Insert Image**

In following the instructions, skip steps 7 - 12 in the *Create an IoT Edge module project* section and use the code from [this location](https://raw.githubusercontent.com/iizotov/iot-workshop/master/src/iotedgeblemodule/Program.cs) instead
* For simplicity, use Docker Hub for storing your Docker image. For even greater simplicity, I have done all the steps above, and created and pushed the image onto my public Docker Hub, it is called [faister/iotedgeblemodule:1.0-preview](https://hub.docker.com/r/faister/iotedgeblemodule/tags/)
* Since you'll be running this module on an ARM-based platform, change the `FROM ...` line in `/Docker/linux-x64/Dockerfile` to 
    ```
    FROM microsoft/dotnet:2.0.0-runtime-stretch-arm32v7
    ```
* Since your code will be interfacing with Redis, add a dependency on the `StackExchange.Redis` Nuget package to the `.cproj` file:
    ```
    <PackageReference Include="StackExchange.Redis" Version="1.2.6" />
    ```
* Before deploying the module, grab the Docker gateway IP address (that is, the address that your module will use to connect to Redis) by running
    ```bash
    sudo docker network inspect --format='{{json .IPAM.Config}}' azure-iot-edge
    ```
* When deploying the module, specify the following desired properties in the module twin (replace `<ip>` with the gateway ip address)
    ```json
    {
        "properties.desired": {
            "redisIPAddress":"<ip>",
            "redisChannelName": "data"
        }
    }
    ```
* When deploying the module, specify the following route (replace `<module>` with the name of your module)
    ```json
    {
        "routes": {
            "route": "FROM /messages/modules/<module>/outputs/* INTO $upstream"
        }
    }
    ```
* You may delete the tempSensor module deployed earlier because you do not need it in the following steps.
* Make sure the module is deployed correctly through Azure Portal and by running `sudo watch docker ps`
* You can review the container's logs by running `sudo docker logs -f <container_id>`
* Whilst following the logs, change the name of the Redis channel by submitting an update to  set of desired properties via Azure Portal
* In a separate SSH session, launch the python script to start pushing telemetry messages into the Redis instance. Replace `<mac address>` with the MAC address of your Sentor Tag and `<ip>` with the Docker gateway ip address from one of the steps above:
    ```bash
    while true; do python ~pi/sensortag.py --all -t 0.5 --redisip <ip> <mac address> --redischannel data; sleep 1; done
    ```
    The infinite loop is used to reconnect when your Sensor Tag enters power saving mode. If you want to stop the script, hold Ctrl+C continuously 
    > If something's wrong, bounce bluetooth by running
`sudo /etc/init.d/bluetooth restart`
* Connect to the IoT Hub using either [Azure IoT Device Explorer](https://github.com/Azure/azure-iot-sdk-csharp/releases) or [iothub-explorer](https://github.com/Azure/iothub-explorer) and monitor the telemetry flowing through

## Lab 3: Deploy Azure Stream Analytics on IoT Edge
TODO, unfinished
```

use https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-edge

create input input1
create output alert
create output cloud

define query: 
SELECT
    *
INTO
    [cloud]
FROM
    [input1]

SELECT
    'temperature alert' as alert_message,
    '30' as temp_ir_threshold,
    temp_ir,
    tag,
    [timestamp]
INTO
    [alert]
FROM
    [input1]
WHERE
    [input1].[temp_ir] >= 30.0
	
Create anew storage account and a private container for the ASA job,
Add a new ASA module to the edge, rename it to ASA or copy the auto-generated name (in which case adjust the routes below accordingly)

{
"routes": {                                              
	"sensorToAsa":   "FROM /messages/modules/iotedgeblemodule/* INTO BrokeredEndpoint(\"/modules/{ASA}/inputs/input1\")",
	"messagesToCloud": "FROM /messages/modules/{ASA}/outputs/cloud INTO $upstream",
	"AlertsToCloud": "FROM /messages/modules/{ASA}/outputs/alert INTO $upstream"

}
} 
```

# Additional Challenges
TODO
### Create an Stream Analytics Query in Azure
In addition to running ASA on the Edge, you can run it in the cloud 
- Create an Azure Stream Analytics query that selects all the data from your IoT Hub and outputs the results to Power BI, displaying aggregate metrics and sending alerts (e.g. when temperature exceeds 37 degrees for longer that 15 consecutive seconds). Experiment with the ASA windowing functions and Azure Logic Apps to achieve it.
- How about making this temperature threshold dynamic? Hint: store it as [reference data](https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-use-reference-data) and use it in your queries.

### Create a Power BI Dashboard
- Create a [Power BI](http://app.powerbi.com) Dashboard that visualizes your Sensor Tag data in creative ways. Feel free to use any of the Power BI Custom Visuals available [here](https://store.office.com/en-us/appshome.aspx?productgroup=PowerBI). You can learn how to create Power BI Dashboards from a Stream Analytics Output [here](https://azure.microsoft.com/en-us/documentation/articles/stream-analytics-power-bi-dashboard/).

### Time Series Insights
- Create a [Time Series Insights](https://azure.microsoft.com/en-us/services/time-series-insights/) environment and connect it to the Azure IoT Hub


## Disclaimer
The code included in this workshop is not intended to be used in production. This is beyond the scope of this learning experience.

## Related Links and Acknowledgments
* Sensor Tag Bluepy [Example](https://gist.github.com/atotto/ae603b962115eef703c0011d8e652ea3) by @atotto

## Contributors
* Fai Lai
* Igor Izotov
