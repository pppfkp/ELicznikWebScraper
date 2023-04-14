# ELicznikWebscraper
## Introduction
This is a library written in *C#* that is avaiable as a **NuGet package** (https://www.nuget.org/packages/ELicznikScraper). It is a part of my larger project I'm developing to help my dad, who needed an app to perform calculations on energy consumption and production from his PV panels.
## Installation
The installation proccess is actually pretty straightforward- you need to add the ELicznikScraper package to your *.net* project. 
You can do it by using dotnet cli:
`dotnet add package ELicznikScraper --version 1.0.1`
You can also use a *NuGet package manager*:

![image](https://user-images.githubusercontent.com/64775002/232020451-49bd1bcd-27d2-4b22-ae73-79bad48ebb7b.png)

## Usage
After you added the package to your project you would gain access to `ELicznik` class.

```
//Create an ELicznik instance 
ELicznik eLicznik = new("email", "password");
```
It would log in automatically. 
Then you can use your instance to get the data.
```
var data = eLicznik.GetData();
```
It would return by default *JSON* data from yesterday.
Example data:

```
[
	  {
	    "Data": "2023-04-13 1:00",
	    "Wartosc kWh": "0.136",
	    "Rodzaj": "pobór"
	  },
	...
	  {
	    "Data": "2023-04-13 24:00",
	    "Wartosc kWh": "0.000",
	    "Rodzaj": "oddanie"
	  }
]
```
In `GetData()` call you can specify time period, desired format, specify the type of data that you want to obtain and the  frequency of a readings.
It was accomplished by using enums.
Data formats: 
```
    public enum FileType
    {
        CSV,
        JSON
    }
```
Frequency of readings: 
```
    public enum DataFrequency
    {
        Hourly,
        Daily
    }
```
Data type:
```
    public enum DataScope
    {
        ConsumptionOnly,
        ProductionOnly,
        ConsumptionAndProduction
    }
```
You simply need to provide this values in the `GetData()` method call along with the starting and ending dates (type of `DateOnly`).
Here is an example call:
```
var dateFrom = new DateOnly(2023, 01, 01);
var dateTo = new DateOnly(2023, 01, 31);
var data = elicznik.GetData(dateFrom, dateTo, DataFrequency.Hourly, FileType.JSON, DataScope.ConsumptionAndProduction);
```
It gets both consumption and production json format data from Jan 1st 2023 to Jan 31st 2023 in hourly intervals.
## Contribution
Feel free to ask me anything! You can contact me at pppfkp@outlook.com
I'm open to any suggestions, and if you know how to **do something better** please let me know!
