# AVREmulator
This is an AVR Emulator written for Fun, this emulator currently build For Atmega32, in future I hope to make it general to work with all AVR family.

The resouse of information that i follow to build this emulator is
1. book : `The AVR Microcontroller and Embedded Systems Using Assembly and C` By Muhammad Ali Mazidi
2. [AVR opcode summary][1]
3. [AVR instruction set manual][2]

## install
For now there is no UI interface for the project (it will be added soon) but you can download the source code and run the tests and play around with it 😅😉

to be able to run the code, you need the Visual Studio 22 or .net 6 SDK to be installed in your machine 
1. Download the project 
```cmd
git clone https://github.com/abramReda/AVREmulator.git
```
2. inside the project folder you can found `AVREmulator.sln` file open it with vs22
3. to see see tests result within VS22 right click on prject name `AvREmulatorTests` and select Run Tests Then open test explorer window to see the result

![Screenshot 2022-05-21 230712](https://user-images.githubusercontent.com/37075700/169669009-4cceb23a-9aa6-495d-9184-f3d43d583431.png)

if you don't have vs22 you can run the test from termnal or cmd by run the following command
```cmd
dotnet test
```
![Capture](https://user-images.githubusercontent.com/37075700/169669254-e836647b-1081-497b-888e-968a49700203.PNG)


[1]: http://lyons42.com/AVR/Opcodes/AVRAllOpcodes.html#Block48
[2]: http://ww1.microchip.com/downloads/en/devicedoc/atmel-0856-avr-instruction-set-manual.pdf
