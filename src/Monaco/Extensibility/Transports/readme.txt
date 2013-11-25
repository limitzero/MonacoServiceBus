How to create a transport and install it into the service bus
======================================

All external (custom) transports are indentified by the marker class BaseTransportBootstrapper. 

1. Define the xml structure that best fits how to configure your transport.
2. Create a concrete implementation of the class BaseTransportBootstrapper and override the ExtractElementSectionToConfigureTransport and ConfigureTransportDependencies
methods to read your custom settings and then add into the container any transport dependencies. 
3. 