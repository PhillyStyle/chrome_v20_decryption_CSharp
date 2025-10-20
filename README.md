# chrome_v20_decryption_CSharp
**chrome_v20_decryption_CSharp** is a Chrome v20 decrypter.
It can retrieve the following:
- Login Info (passwords)
- Cookies
- History
- Downloads
- Credit Cards

This program is not malicious in nature. It simply reads data, decrypts it if needed, and prints it to screen.
Note: **Must Be Ran as Administrator to decrypt Passwords, Cookies, or Credit Cards.**

## Update
Updated for better Chromium browser support.
Now Supports some browsers that don't use the exact same v20 encryption as Chrome.

Now supports the following browsers: (in no particular order)

* Arc
* Vivaldi
* Chrome
* Chrome Beta
* Chrome Dev
* Edge
* Brave
* Chromium
* DuckDuckGo (cookies only)
* Opera
* Opera-GX

## Compiling
This is coded C# 4.8 and uses the following NuGet packages:
* BouncyCastle.Cryptography 2.6.2
* Costura.Fody
* Fody

Fody and Costura.Fody are optional, that is just what merges the BouncyCastle into the executable to make it a standalone (rather large one at that) exe.

I went with C# 4.8 for Windows compatibility reasons.

## Usage:
* No command line arguments = Get All (passwords, cookies, history, and downloads)
* -p or --passwords = Get Login Info
* -c or --cookies = Get Cookies
* -h or --history = Get History
* -d or --downloads = Get Downloads
* -cc or --creditcards = Get Credit Cards

I made this program to help the community.  The only good Chrome v20 decryption code I could find was written in Python from [https://github.com/runassu/chrome_v20_decryption](https://github.com/runassu/chrome_v20_decryption).  I decided it would help the community a lot if there was some Chrome v20 decryption code written in C#.  So here it is.

## Credits
I want to give credit to the following for playing their part in this code being created:

* [https://github.com/runassu/chrome_v20_decryption](https://github.com/runassu/chrome_v20_decryption) for python decryption code.
* [https://github.com/moom825/xeno-rat](https://github.com/moom825/xeno-rat) for InfoGrab code. (Xeno-Rat plugin)
* [https://chatgpt.com/](https://chatgpt.com/) for help translating the python decryption code to C#.

## Other
Currently the compiled executable is detected as a virus by Windows Defender, because AV doesn't like the SQLiteReader.cs file, because similar code has been used in many password stealing malware.  So I am posting code only.  No Executable, because I don't want Github to take this down because it thinks it is a virus.
