##Custom Reactions
###Important
*	For modifying **global** custom reactions, the ones which will work across all the servers your bot is connected to, you **must** be a Bot Owner.  
You must also use the commands for adding, deleting and listing these reactions in a direct message with the bot.  
*	For modifying **local** custom reactions, the ones which will only work on the server that they are added on, it is required to have the **Administrator** permission.  
You must also use the commands for adding, deleting and listing these reactions in the server you want the custom reactions to work on.  

###Commands and Their Use

| Command Name | Description | Example |
|:------------:|-------------|---------|
|`.acr`|Add a custom reaction with a trigger and a response. Running this command in a server requries the Administrator permission. Running this command in DM is Bot Owner only, and adds a new global custom reaction. Guide [here](Coming Soon)|`.acr "hello" Hi there, %user%!`|
|`.lcr`|Lists a page of global or server custom reactions (15 reactions per page). Running this command in a DM will list the global custom reactions, while running it in a server will list that server's custom reactions.|`.lcr 1`|
|`.dcr`|Deletes a custom reaction based on the provided index. Running this command in a server requires the Administrator permission. Running this command in DM is Bot Owner only, and will delete a global custom reaction.|`.dcr 5`|


####Now that we know the commands let's take a look at an example of adding a command with `.acr`,  
`.acr "Nice Weather" It sure is, %user%!`  

This command can be split into two different arguments:  

* 	 The trigger, `"Nice Weather"`  
* 	 And the response, `It sure is, %user%!`  

An important thing to note about the triger is that, to be more than one word, we had to wrap it with quotation marks, `"Like this"` otherwise, only the first word would have been recognised as the trigger, and the second word would have been recognised as part of the response.  

There's no special requirement for the formatting of the response, so we could just write it in exactly the same way we want it to respond, albeit with a placeholder - which will be explained in this next section.  

Now, if that command was ran in a server, anyone on that server can make the bot mention them, saying `It sure is, @Username` anytime they say "Nice Weather". If the command is ran in a direct message with the bot, then the custom reaction can be used on every server the bot is connected to.  

###Placeholders!
There are currently three different placeholders which we will look at, with more placeholders potentially coming in the future.  

| Placeholder | Description | Example Usage | Usage |
|:-----------:|-------------|---------------|-------|
|`%mention`|The `%mention%` placeholder is triggered when you type `@BotName` - It's important to note that if you've given the bot a custom nickname, this trigger won't work!|```.acr "Hello %mention%" I,  %mention%, also say hello!```|Input: "Hello @BotName" Output: "I, @BotName, also say hello!"|
|`%user%`|The `%user%` placeholder mentions the person who said the command|`.acr "Who am I?" You are %user%!`|Input: "Who am I?" Output: "You are @Username!"|
|`%rng%`|The `%rng%` placeholder generates a random number between 0 and 10|`.acr "Random number" %rng%`|Input: "Random number" Output: "2"|
|`%target%`|The `%target%` placeholder is used to make WizBot Mention another person or phrase, it is only supported as part of the response|`.acr "Say this: " %target%`|Input: "Say this: I, @BotName, am a parrot!". Output: "I, @BotName, am a parrot!".|

 Thanks to Nekai for being creative. <3
