# ![Product Logo](https://raw.githubusercontent.com/gavin1970/Chizl.JsonTables/refs/heads/master/Chizl.JsonTables/imgs/Chizl.JsonTables_200.png)
# Chizl.JsonTables

<!-- [![NuGet version (Chizl.JsonTables)](https://img.shields.io/nuget/v/Chizl.JsonTables.svg?style=flat-square)](https://www.nuget.org/packages/Chizl.JsonTables/) -->

Chizl.JsonTables uses the popular Newtonsoft.Json Library with an extention of DataSets and DataTables.  You use this library as you would DataSets and it produces JSON including schemas of your dataset for you.  All data is in memory as a DataSet to make it fast and auto updates the Json for all data changes.  Structure changes require a Flush().
This libary also supports SecuredString Columns.  This means, you can have a PI data in memory that is secured and AES encrypted that is converted to Base64 strings in your Json.  This is done via AES Encryption with salted passwords you pass into constructor.  

### The following is not recommended
Salted passwords are not required, however default Key/Vector will be used for encrypting any SecureString columns in JSON if you do not provide one.

## Build with
- Microsoft Visual Studio Professional 2022 (64-bit) Version 17.13.6
 
## Compiles as
- ![NetFramework](https://img.shields.io/badge/.NET_Framework-v4.7_v4.8-blue)
- ![NetStandard](https://img.shields.io/badge/.NET_Standard-v2.0_v2.1-blue)
- ![NetCore](https://img.shields.io/badge/.NET-v6.0_v8.0_v9.0_-blue)

## Dependency
- ![Newtonsoft.Json](https://img.shields.io/badge/Newtonsoft.Json-v13.0.3-blue)

## Example of use
- [net6.0 Demo Included](https://github.com/gavin1970/Chizl.JsonTables/tree/master/DemoConsole)

## Links
<!-- - [Homepage] <- being reworked <!--(http://www.chizl.com/Chizl.JsonTables)-->
<!-- - [Documentation] <- being reworked <!--(http://www.chizl.com/Chizl.JsonTables/help)-->
<!-- - [NuGet Package] <- being setup <!--(https://www.nuget.org/packages/Chizl.JsonTables)-->
<!-- - [Release Notes] <- being reworked <!--(https://github.com/gavin1970/Chizl.JsonTables/releases)-->
<!-- - [Contributing Guidelines] <- being reworked <!--(https://github.com/gavin1970/Chizl.JsonTables/blob/master/CONTRIBUTING.md)-->
- [License](https://github.com/gavin1970/Chizl.JsonTables/blob/master/Chizl.JsonTables/docs/LICENSE.md)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/Chizl.JsonTables)
