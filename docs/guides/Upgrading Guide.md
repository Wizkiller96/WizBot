##Upgrading WizBot from an older release

**If you have WizBot 0.9x**

- Follow the [Windows Guide](N/A)/[Linux Guide](N/A)/[OS X Guide](N/A) and install **WizBot 1.0**.
- Navigate to your **old** `WizBot` folder and copy `credentials.json` file and the `data` folder.
- Paste them into **WizBot 1.0** `/WizBot/src/WizBot/` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** WizBot as the guide describes, if it is not already running.
- In any channel, run the `.migratedata` [command](N/A) and Nadeko will start migrating your old data.
- Once that is done **restart** WizBot and everything should work as expected!
