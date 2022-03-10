# OPC-XML DA CONNECTOR

App for extend connection capability to opc-xml da device enabled

- [OPC-XML DA CONNECTOR](#opc-xml-da-connector)
  - [Introduction](#introduction)
    - [Before starting](#before-starting)
  - [Requirements](#requirements)
    - [Used components](#used-components)
    - [Hardware Requirements](#hardware-requirements)
    - [Software requirements](#software-requirements)
  - [Installation](#installation)
    - [Download the application](#download-the-application)
    - [Import an application in .app format](#import-an-application-in-app-format)
    - [Create a new standalone application](#create-a-new-standalone-application)
    - [Upload the application to Industrial Edge Management](#upload-the-application-to-industrial-edge-management)
      - [Link your Industrial Edge App Publisher](#link-your-industrial-edge-app-publisher)
      - [Import a standalone application into Industrial Edge Management](#import-a-standalone-application-into-industrial-edge-management)
  - [Configuration](#configuration)
  - [Usage](#usage)
  - [Build](#build)
    - [Docker-file](#docker-file)
    - [Docker-Compose](#docker-compose)
  - [Structured Results](#structured-results)
  - [Documentation](#documentation)
  - [Contribution](#contribution)
  - [License & Legal Information](#license--legal-information)

## Introduction

The opc-xml da connector extend the connectivity of the Siemens Industrial Edge Platform, allowing to retrive data from devices where opc-xml da is the only choice for sending data. It borns mainly for solving the problem of connecting Siemens Simotion Devices with deprecated firmware (v4.4).
As connector, the application will provide data in the form of json messages, under custom defined MQTT topic. The storage of collected information could be then manipulated, i.e. with [Industrial Edge Flow Creator](#documentation), for possible long term historicization.


### Before starting

This guide describes how to use and install the opc-xml da connector.

Check the requirements in the [Requirements](#requirements) section before proceeding with the installation. Details for the installation procedure can be found in the [Installation](#installation) section.

For details on how to use the Industrial Edge Flow Creator service see the Using section and for all online references on using it see the [Documentation](#documentation) section.

The application comes with some [Application Examples](#application-examples) in the dedicated section.

The [Build](#build) section shows in detail how this application was built using the Docker environment

## Requirements

### Used components

- OS: Windows or Linux
- Visual Studio Professionals 2019
- Docker minimum V18.09
- Docker Compose V2.0 - V2.4
- Industrial Edge App Publisher (IEAP) V1.4.3
- Industrial Edge Management (IEM) V1.4.11
- Industrial Edge Device (IED) V1.5.0-21

### Hardware Requirements

The opc-xml da application is only compatible with SIEMENS devices that have Industrial Edge functionality enabled.

### Software requirements

The edge-grafana application needs 100 MB of RAM to run:

| Service Name | Memory Limit |
|--------------|--------------|
| opc-xml da connector | 100 MB |


> **Note:** This limit has been set after testing the connection on a low-medium number of devices,but can be modified according to your needs by acting on the docker-compose file and then on the app configuration in the Edge App Publisher software, creating a custom version of this application.

## Installation

Below you will find the steps required to download the pre-compiled app or to create and install an edge app from the source code provided here.

You can either import a directly downloadable .app file below, or use the provided source code to build a new app from scratch.

Please refer to the [Documentation](#documentation) section for detailed information on Industrial Edge application development.

### Download the application

The **edge-grafana** application can be downloaded in .app format using this secure Google Drive link:

- [opc-xml-da-connector_0.0.20.app](https://drive.google.com/file/d/1H1TJo-yjp0q9MBLdkMAfUHyM3WOhYFN5/view?usp=sharing)

### Import an application in .app format

- Open the **Industrial Edge App Publisher** software
- Import the `opc-xml-da-connector_0.0.20.app` file using the **Import** button
- The new imported application will appear in the **Standalone Applications** section

### Create a new standalone application

- Open the **Industrial Edge App Publisher** software
- Go to the **Standalone Applications** section and create a new application
- Import the [docker-compose](docker-compose.yml) file using the **Import YAML** button
- Click on **Review** and then on **Validate & Create**.

### Upload the application to Industrial Edge Management

Below is a brief description on how to publish your application to your IEM.

For more detailed information please see the official Industrial Edge GitHub guide to [uploading apps to the IEM](https://github.com/industrial-edge/upload-app-to-industrial-edge-management) and the [Documentation](#documentation) section.

#### Link your Industrial Edge App Publisher

- Connect your Industrial Edge App Publisher to your **Docker Engine**
- Connect your Industrial Edge App Publisher to your **Industrial Edge Management**

#### Import a standalone application into Industrial Edge Management

- Create a new **Apps project** in the connected IEM or select an existing one
- Import the app version created in the **Standalone Applications** section into the selected IEM project
- Press **Start Upload** to transfer the application into Industrial Edge Management

## Configuration

To configure this app, two configuration files are needed:

- the [config.json](cfg-data/config.json)
- the [mqtt-config.json](cfg-data/mqtt-config.json)

both deployed togheter with the app.
Below an example of the two configuration files:

```json
#config.json
[
  ##### First Device ######
  {
    "ID" :  0,
    "IP": "192.168.1.2",
    "USER": "simotion",
    "PASS": "simotion",
    "VARIABLES": [ "unit/Programm.variabile"]
  }
  ##### Second Device ######
  { 
    "ID" :  1,
    "IP": "192.168.1.3",
    "USER": "simotion",
    "PASS": "simotion",
    "VARIABLES": [ "unit/Programm.variabile2" ]
  }
]
```

```json
#mqtt-config.json
{
    "MQTT_USER":"user",
    "MQTT_PASSWORD":"password",
    "MQTT_IP":"ie-databus",
    "PUB_TOPIC":"ie/opcxml",
    "SUB_TOPIC":"topic2"
}
```

## Usage

Based on the first configuration above [config.json](cfg-data/config.json), the app is able to establish the connection with the listed device, searching for the list of variables requested.
The browsing of the variable is possible due to the simotion property of making visible the variables requested. Thus, is needed from the user a prerequisite know-how of the data to collect, since browsing of the variables is not yet implemented in the app.
With the second configuration [mqtt-config.json](cfg-data/mqtt-config.json), the app will create an **MQTT CLIENT** for the connection with the broker **Industrial Edge Databus** publishing the required data on the **PUB_TOPIC**

## Build

The edge-grafana application is built from a Docker Alpine image where a version of Grafana is installed with some customizations such as preconfigured datasources provisioning, dedicated settings for operation and the industrialedge-dataservice-datasource plugin.

Refer to the [Dockerfile](grafana/Dockerfile) used for building the image.

### Docker-file

The creation of docker file can be easily fullfilled thanks to the Add-on present in any of the latest Visual Studio version.
For Creating it from the scretch, please look at the code below where the dotnet structure is restored for the publishing of the .dll builded.
```Dockerfile
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base

WORKDIR /app



FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build

WORKDIR /src

COPY ["opc-xml-da-connector/opc-xml-da-connector.csproj", "opc-xml-da-connector/"]

RUN dotnet restore "opc-xml-da-connector/opc-xml-da-connector.csproj"

COPY . .

WORKDIR "/src/opc-xml-da-connector"

RUN dotnet build "opc-xml-da-connector.csproj" -c Release -o /app/build



FROM build AS publish

RUN dotnet publish "opc-xml-da-connector.csproj" -c Release -o /app/publish



FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "opc-xml-da-connector.dll"]
```
Hence, is important that in the structure of the folder is present the `.csproj` file that build the entire solution inside the dotnet image

### Docker-Compose

Below are then presented the structure of the `docker-compose.yaml` that needs the definition of the **L2 network** for establishing correctly the connection to the opc-xml-da server present at simotion side.

```yaml
version: '2.4'

services:

  opc-xml-da-connector:

    image: opc-xml-da-connector

    build:

      context: .

      dockerfile: opc-xml-da-connector/Dockerfile

    volumes:

      - ./cfg-data/:/app/cfg-data/ 

    cap_add:

      - NET_ADMIN

    networks:

      - proxy-redirect

      - zzz_layer2_net1

networks:

## Network for accessing INDUSTRIAL EDGE DATABUS ##

  proxy-redirect:

    external: true

    name: proxy-redirect

## Network L2 for accessing OPC-XML-DA DEVICE ##

  zzz_layer2_net1:

    external: true

    name: zzz_layer2_net1
```
## Structured Results

The data will be published to the topic `<PUB_TOPIC>/` every `100` milliseconds.

For example, based on the configuration above, the data received for datasource on topic **ie/opcxml** will be like:
```json
{
  "SimotionID" : "0",
  "datapointDefinition" : [
    {
      "Date" : "1992-02-10T22:11:38.08+00.00",
      "Value" : "219",
      "ItemName" : "unit/Programm.variabile",
      "ItemPath" : "SIMOTION"
    }
  ]
}
```
## Documentation

You can find further documentation and help about Industrial Edge in the following links:
- [SIOS - Industrial Edge Flow Creator](https://cache.industry.siemens.com/dl/files/331/109794331/att_1057284/v1/IE_flow_creator_operation_en-US.pdf)
- [SIOS - DataService Application Manual](https://support.industry.siemens.com/cs/ww/en/view/109781417)
- [SIOS - Dataservice Development Kit](https://support.industry.siemens.com/cs/ww/en/view/109792717)

You can find further documentation and help about Industrial Edge in the following links:

- [Industrial Edge Hub](https://iehub.eu1.edge.siemens.cloud/#/documentation)
- [Industrial Edge Forum](https://www.siemens.com/industrial-edge-forum)
- [Industrial Edge landing page](https://new.siemens.com/global/en/products/automation/topic-areas/industrial-edge/simatic-edge.html)
- [Industrial Edge GitHub page](https://github.com/industrial-edge)
- [Industrial Edge App Developer Guide](https://support.industry.siemens.com/cs/ww/en/view/109795865)

## Contribution

Thanks for your interest in contributing. Anybody is free to report bugs, unclear documentation, and other problems regarding this repository in the Issues section or, even better, is free to propose any changes to this repository using Merge Requests.

## License & Legal Information

Please read the [Legal Information](LICENSE.md).
