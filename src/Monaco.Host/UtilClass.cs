using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;

namespace Monaco.Host
{
    public static class UtilClass
    {    
        // Valid parameters forms:    
        // {-,/,--}param{ ,=,:}((",')value(",'))    
        // Examples:     // -param1 value1 --param2 /param3:"Test-:-work"     
        //   /param4=happy -param5 '--=nice=--'    
        public static StringDictionary SplitArgString(this string[] Args)   
        {       
            StringDictionary parameters = new StringDictionary();        

            // Define -, --, /, : and = as valid delimiters.  Ignore : and = if enclosed in quotes.        
            Regex validDelims = new Regex(@"^-{1,2}|^/|[^['""]?.*]=['""]?$|[^['""]?.*]:['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);        

            // Define anything enclosed with double quotes as a match.  We'll use this to replace       
            // the entire string with only the part that matches (everything but the quotes)        
            Regex quotedString = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);        
            string currentParam = null;        
            string[] parts;        
            
            foreach (string arg in Args)       
            {            
                // Apply validDelims to current arg to see how many significant characters were present            
                // We're limiting to 3 to forcefully ignore any characters in the parameter VALUE            
                // that would normally be used as a delimiter            
                parts = validDelims.Split(arg, 3);            
                
                switch (parts.Length)            
                {                
                    // no special characters present.  we assume this means that this part               
                    // represents a value to the previously provided parameter.                
                    // For example, if we have: "--MyTestArg myValue"                
                    // currentParam would currently be set to "--MyTestArg"                
                    // parts[0] would hold "myValue", to be assigned to MyTestArg                
                    
                    case 1:                    
                        if (currentParam != null)
                        {
                            if (!parameters.ContainsKey(currentParam))                          
                                parameters.Add(currentParam, quotedString.Replace(parts[0], "$1"));                      
                               currentParam = null;
                        }                
                        break;               
                    
                    // One split ocurred, meaning we found a parameter delimiter                
                    // at the start of arg, but nothing to denote a value.                
                    // example: --MyParam                
                    
                    case 2:                    
                    // We already had a parameter with no value last time through the loop.                    
                    // That means we have no explicit value to give currentParam. We'll default it to "true"                    
                    if (currentParam != null && !parameters.ContainsKey(currentParam))                        
                        parameters.Add(currentParam, "true");                   
                    // Store our value-less param and grab the next arg to see if it has our value                    
                    // parts[0] only contains the opening delimiter -, --, or /,                     
                    // so we go after parts[1] for the actual param name                    
                    currentParam = parts[1];                    
                    break;                
                    
                    // Two splits occurred.  We found a starting parameter delimiter,                
                    // a parameter name, and another delimiter denoting a value for this parameter                
                    // Example: --MyParam=MyValue   or   --MyParam:MyValue                
                    case 3:                   
                    // We already had a parameter with no value last time through the loop.                    
                    // That means we have no explicit value to give currentParam. We'll default it to "true"                    
                    if (currentParam != null && !parameters.ContainsKey(currentParam))                        
                        parameters.Add(currentParam, "true");                    
                    // Store the good param name                    
                    currentParam = parts[1];                   
                    // Ignores parameters that have already been presented, not thrilled about this approach...                   
                    if (!parameters.ContainsKey(currentParam))                        
                        parameters.Add(currentParam, quotedString.Replace(parts[2], "$1"));                    
                    // Reset currentParam, we already have both parameter and value for this arg                    
                    currentParam = null;                    
                    break;           
                }       
            }        
            // Final cleanup, we may still have a parameter at the end of the args string that didn't get a value        
            if (currentParam != null)
            {
                if (!parameters.ContainsKey(currentParam))                
                    parameters.Add(currentParam, "true");
            }       
            return parameters;    
        }

		public static void ScanForEndpointConfiguration(ref string serviceName, ref Assembly assembly)
		{
			serviceName = string.Empty;
			assembly = null;

			var files = System.IO.Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "*.dll");
			
			Type endpoint = null;

			foreach (var file in files)
			{
				try
				{
					assembly = Assembly.LoadFile(file);

					if (assembly == null) continue;

					endpoint = (from match in assembly.GetTypes()
								where typeof(BaseEndpointConfiguration).IsAssignableFrom(match)
								select match).FirstOrDefault();

					if (endpoint != null) break;

				}
				catch (Exception)
				{
					continue;
				}

				if (endpoint == null)
					throw new Exception("The service can not find a class for defining the endpoint. " +
						"Please create a class to define the endpoint configuration for your service " + assembly.GetName().Name);
				else
				{
					serviceName = assembly.GetName().Name;
				}
			}
		}
    }
}
