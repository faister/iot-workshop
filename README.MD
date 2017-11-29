# IoT Workshop 2017 v2 - Australia 

Welcome to our IoT workshop! 

We are so glad to have you with us today. We put together this workshop to give you some hands-on experience with the Microsoft Azure IoT suite. Our goal is to get you up-to-speed on the latest developments so you can take the knowledge back to your office and start implementing IoT solutions with them.

>Please note that the contents of this workshop have been updated to reflect the [latest changes](https://github.com/Azure/iot-edge) to IoT Edge. The old content is still available in [/v1](./v1/README.md)

## Prerequisites and Course Materials
This is a technical workshop and you're expected to be comfortable writing and understanding code (we will chiefly use .NET Core). Also, you're expected to be comfortable around Linux, be able use SSH, bash, Visual Studio Code and know how to use text editors (such as **vi** or **nano**). Basic understanding of Docker Containers and Azure is essential, but not critical.

Please make sure to complete the following prerequisites prior to coming to the workshop
- [Install](http://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html) PuTTY (Windows users only)
- [Download](https://github.com/Azure/azure-iot-sdk-csharp/releases) Azure IoT Device Explorer (Windows only) or [Install](https://github.com/Azure/iothub-explorer) iothub-explorer, a command line utility  
- [Activate](https://azure.microsoft.com/en-au/free/) a free trial Azure subscription and familiarise yourself with the [Azure Portal](https://portal.azure.com/)
- [Install](https://code.visualstudio.com/docs/setup/setup-overview) Visual Studio Code
- [Install](https://www.microsoft.com/net/learn/get-started/windows#linuxredhat_2) .NET SDK on your machine
- [Register](https://hub.docker.com/) on Docker Hub
- [Install](https://www.docker.com/community-edition) Docker Community Edition

## Agenda
Day 1: 
- Azure IoT Suite: Level 300 intro 
- Lab 0: Environment setup: azure subscription, SSH access to your Raspberry PI, set up prerequisites
- Lab 1: Deploy Simulated Device on IoT Edge
- Lab 2: Develop and deploy a custom C# module to connect to Sensor Tag  on IoT Edge

Day 2:
- Lab 3: Deploy Azure Stream Analytics on IoT Edge
- Time for guided experimentation
- Wrap-up

## Getting Started
Each group will be provided with a Raspberry Pi 3 running [Raspbian Stretch with Desktop](https://www.raspberrypi.org/downloads/raspbian/), a Texas Instruments(TI) Bluetooth Low Energy (BLE) Sensor Tag, and an Azure Subscription.

> Proctors are available to help you work through these workshops, answer questions or talk tech.

### What's in the Pre-baked Image
For your convenience we pre-baked an image that you can [download](https://iizotovshare.blob.core.windows.net/downloads/v2/image16G.7z) and try at home. This image is based on stock standard [Raspbian Stretch with Desktop](https://www.raspberrypi.org/downloads/raspbian/) with the following tweaks:
- VNC server has been enabled, you can access you RPI's GUI using [VNC Viewer](https://www.realvnc.com/download/viewer/)
- Crontab [script](./src/aux-files/mailip) notifying proctors when raspberry PIs receive or refresh an IP address to simplify the classroom process
- SSH enabled
- Swap increased to 1G

## Lab 0: Environment Setup
1. Make sure you can access [Azure Portal](https://portal.azure.com/) and have an active Azure Subscription
    > speak to one of the proctors if you need an Azure Subscription or [activate](https://azure.microsoft.com/en-au/free/) a free trial yourself
2. The procrotors will tell you the IP address of your Raspberry PI, make sure you can access it via SSH and VNC
    > username: **pi**, password: **raspberry**
3. Install [Docker](https://www.raspberrypi.org/blog/docker-comes-to-raspberry-pi/) on your Raspberry PI by running
    ```bash
    curl -sSL https://get.docker.com | sh
    ```
    * Check docker instalation by running

    ```bash
    sudo docker run hello-world
    ```
4. Install [bluepy](https://github.com/IanHarvey/bluepy) - a Python interface to Bluetooth LE on Linux
    ```bash
    sudo apt-get -y install python-pip libglib2.0-dev python-dev libffi-dev libssl-dev
    sudo pip install bluepy
    ```
5. Install [redis](https://redis.io/) - redis will be used as a pub/sub mechanism between the Sensor Tag (accessed via bluepy) and the IoT Edge
    ```bash
    sudo apt-get -y install redis-server
    sudo pip install redis
    ``` 
    * edit `/etc/redis/redis.conf`, commenting out the bind line to make Redis listen on all available ip addresses, adding a new line: `maxmemory 512mb`, and setting `protected-mode` to `no`. Save the config file
    * If you're feeling a little bit lazy, replace a configuration file by running
        ```bash
        sudo mv /etc/redis/redis.conf /etc/redis/redis.conf.default 
        curl -sSL https://raw.githubusercontent.com/iizotov/iot-workshop/master/src/aux-files/redis.conf | sudo tee /etc/redis/redis.conf
        ```
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
    * Note the mac address of your Sensor Tag and launch the Python script by running
        ```bash
        python ~pi/sensortag.py --all -t 0.5 --redisip 127.0.0.1 --redischannel data <mac address>
        ```
    * push the power button on your Sensor Tag and you should see telemetry being read off all sensors sequentially every 0.5 seconds and pushed into Redis running on `127.0.0.1`, channel `data`
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
3. Emulate a container crash by noting down the id of the simulated device container: `sudo docker ps` and killing it:
    ```bash
    sudo docker kill <id>
    ```
    * observe how the IoT Edge Runtime heals itself by running `sudo watch docker ps`

## Lab 2: Develop and deploy a custom C# module to connect to Sensor Tag  on IoT Edge
TODO

## Lab 3: Deploy Azure Stream Analytics on IoT Edge
TODO

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