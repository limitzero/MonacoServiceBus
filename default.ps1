#-------------------------------------------------------------------------------  
# basic build file Powershell script for psake
#-------------------------------------------------------------------------------  
properties { 
  $base_dir  = resolve-path .
  $lib_dir = "$base_dir\lib"
  $source_dir = "$base_dir\src"
  $tests_dir = "$base_dir\tests"
  $build_dir = "$base_dir\build" 
  $buildartifacts_dir = "$build_dir\" 
  $sln_file = "$base_dir\Monaco.sln" 
  $test_lib = "Monaco.Tests.dll"
  $version = "1.1.0.0"
  $tools_dir = "$base_dir\tools"
  $release_dir = "$base_dir\release"
} 

#-------------------------------------------------------------------------------  
# entry task to start the build script
#-------------------------------------------------------------------------------  
task default -depends test

#-------------------------------------------------------------------------------  
# clean the "build" directory and make ready for the build actions
#-------------------------------------------------------------------------------  
task clean { 
  remove-item -force -recurse $buildartifacts_dir -ErrorAction SilentlyContinue
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue 
} 

#-------------------------------------------------------------------------------  
# initializes all of the directories and other objects in preparation for the 
# build process
#-------------------------------------------------------------------------------  
task init -depends clean { 
	new-item $release_dir -itemType directory 
	new-item $buildartifacts_dir -itemType directory 
} 
#-------------------------------------------------------------------------------  
# compiles the solution for the test process
#-------------------------------------------------------------------------------  
task compile -depends init { 
  copy-item "$lib_dir\*" $build_dir
  copy-item "$tools_dir\*" $build_dir
  copy-item "$tools_dir\xUnit\*" $build_dir
  exec msbuild /p:OutDir="$buildartifacts_dir" "$sln_file"
} 
#-------------------------------------------------------------------------------  
# task to run all unit tests in the solution
#-------------------------------------------------------------------------------  
task test -depends compile {
  $old = pwd
  cd $build_dir

  # -- grab each test:
   $tests = $test_lib.split(","); 

  # using .NET 4.0 runner for xunit:
  $xunit = "$build_dir\xunit.console.clr4.x86.exe"

  foreach($test in $tests)
  {
	  $library = $test.trim()
	  .$xunit "$build_dir\$library"
  }

  cd $old		
}

task release -depends compile, test {
	$old = pwd
    cd $build_dir
	
	# build Monaco.dll
	Remove-Item Monaco.partial.dll -ErrorAction SilentlyContinue 
	Rename-Item $build_dir\Monaco.dll Monaco.partial.dll
	
	& $tools_dir\ILMerge.exe monaco.partial.dll `
		Moq.dll `
		Castle.Core.dll `
		Castle.DynamicProxy2.dll `
		Castle.MicroKernel.dll `
		Castle.Windsor.dll `
		log4net.dll `
		Polenter.SharpSerializer.dll `
		/out:Monaco.dll `
		/t:library `
	
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to merge assemblies Monaco library!"
    }
	Remove-Item Monaco.partial.dll -ErrorAction SilentlyContinue
	
	# ----------------- build Monaco.Distributor.dll ------------------------
	Remove-Item Monaco.Distributor.partial.dll -ErrorAction SilentlyContinue 
	Rename-Item $build_dir\Monaco.Distributor.dll Monaco.Distributor.partial.dll
	
	& $tools_dir\ILMerge.exe Monaco.Distributor.partial.dll `
		Castle.Core.dll `
		Castle.MicroKernel.dll `
		Castle.Windsor.dll `
		Monaco.dll `
		/out:Monaco.Distributor.dll `
		/t:library `
	
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to merge assemblies Monaco.Distributor library!"
    }
	Remove-Item Monaco.Distributor.partial.dll -ErrorAction SilentlyContinue

	# -------------- build Monaco.Host.exe ------------------- 
	Remove-Item Monaco.Host.partial.exe -ErrorAction SilentlyContinue 
	Rename-Item $build_dir\Monaco.Host.exe Monaco.Host.partial.exe
	
	& $tools_dir\ILMerge.exe Monaco.Host.partial.exe `
		log4net.dll `
		Castle.Core.dll `
		Castle.MicroKernel.dll `
		Castle.Windsor.dll `
		Monaco.dll `
		/out:Monaco.Host.exe `
		/t:exe `
	
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to merge assemblies Monaco.Host executable!"
    }
	Remove-Item Monaco.Host.partial.exe -ErrorAction SilentlyContinue

}


