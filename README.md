# p3dkg


![Background Image](resources/bgpic.png)
![Background Image2](resources/askjfhskd.png)
## Usage
A simple command-line tool to test and confirm the activation behavior of Prepar3D Software, supporting versions **V3(64Bit), V4, and V5** (v6 is not finish yet). You may allow the program to automatically detect and activate the appropriate version and license type, or manually specify the desired Prepar3D version and license type.

## Activation Process
1. Add the following line to your hosts file (located at `C:\Windows\System32\drivers\etc\hosts` on Windows or `/etc/hosts` on Linux):
   ```text
   127.0.0.1         secure.prepar3d.com
   ```

2. Run the program with administrator privileges (use Powershell/cmd). It will automatically detect the Prepar3D installation path and activate it. Alternatively, you can use the `--type` option to specify the license type and the `--version` option to specify the version to activate.
```bash
.\p3dkg.exe --type academic --version v5
```
If you need to activate a different version, you can simply run this program several times.

## Arguments
| Argument         | Short | Description                                                                                  | Values                                             |
|------------------|-------|----------------------------------------------------------------------------------------------|----------------------------------------------------|
| --help           | -h    | Show help                                                                                   |                                                    |
| --type           | -t    | Specify license type (default is auto-detected)                                             | 'academic' / 'professional' / 'professional-plus'  |
| --version        | -v    | Specify Prepar3D version (default is auto-detected)                                         | 'v3' / 'v4' / 'v5'                                 |
| --verbose        | -V    | Enable verbose output (optional)                                                            |                                                    |

## Feedback
- If you are unable to activate the program, **please feedback!** This will help many people in underprivileged regions enjoy this wonderful simulator!
- issues content should include:

	1. The exact command you used to run the program.
	2. Any error messages or output shown in the terminal or output pane.
	3. Any other relevant information or screenshots.

## Warnings
- This program only works on **WINDOWS** systems. BTW, it is tested under **Linux using Wine**.
- *This program is intended for educational purposes only :)*
- *This program is not affiliated with Lockheed Martin or Prepar3D in any way.*

## Development
- This program is developed using **C#** and **.NET 8.0**. If you want to contribute, you are very welcome!

- If you want to compile this program, you need to install the DOTNET 8.0 SDK, and compile it using the following command:
	```bash
	dotnet build -c Release
	```

## Lisence
This program is license by WTFPL (Do What The F*ck You Want To Public License) v2.0, see [LICENSE](LICENSE) for more details.)
