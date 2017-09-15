Prerequisites
- [.net core 1.1.X][.netcore]
- [ffmpeg][ffmpeg] (and added to path) either download or install using your distro's package manager
- [git][git]

*Clone the repo*  
`git clone -b 1.4 https://github.com/Wizkiller96/WizBot`  
`cd WizBot/src/WizBot`  
Edit `credentials.json.` Read the [JSON Explanations](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/) guide if you don't know how to set it up.   

*run*  
`dotnet restore`  
`dotnet run -c Release`  

*when you decide to update in the future (might not work if you've made custom edits to the source, make sure you know how git works)*   
`git pull`  
`dotnet restore`  
`dotnet run -c Release`  

[.netcore]: https://www.microsoft.com/net/download/core#/sdk
[ffmpeg]: http://ffmpeg.zeranoe.com/builds/
[git]: https://git-scm.com/downloads