Monaco - A Service Bus Implementation for .NET
=======

![Monaco]

# LICENSE
Apache 2.0 - see LICENSE

# IMPORTANT
NOTE: If you are looking at the source - please run build.bat before opening the solution. It creates the SolutionVersion.cs file that is necessary for a successful build.

# INFO
## Overview
Monaco is lean service bus implementation for building loosely coupled applications using the .NET framework. It is based on the ideas in MassTransit, NServiceBus and Rhino.ESB, but offers multiple protocol bridging across endpoints, custom pipelines for message translation,  grouping of message consumers into logical "services" for the service bus to host multiple services.

## Getting started with Mass Transit
### Documentation
Documentation is located at [http://masstransit.pbworks.com/](http://masstransit.pbworks.com/).
### Downloads

 Download Binaries from [TeamCity](http://teamcity.codebetter.com/viewType.html?buildTypeId=bt8&tab=buildTypeStatusDiv).

### Source

1. Clone the source down to your machine. 
  `git clone git://github.com/phatboyg/masstransit.git`
2. Run `build.bat`. NOTE: You must have git on the path (open a regular command line and type git).

 
# REQUIREMENTS
* .NET Framework 3.5 

# CREDITS
C# Webserver Project (http://webserver.codeplex.com/) - added support for NVelocity 

