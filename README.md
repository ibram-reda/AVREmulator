# AVREmulator
This is an AVR Emulator written for Fun, this emulator currently build For Atmega32, in future I hope to make it general to work with all AVR family.

## Resources
The resources of information that i follow to build this emulator is
1. book : `The AVR Microcontroller and Embedded Systems Using Assembly and C` By Muhammad Ali Mazidi
2. [AVR opcode summary][1]
3. [AVR instruction set manual][2]

## Install
to be able to run the code, you need the Visual Studio 22 or [.net 7 SDK][5] to be installed in your machine 
1. Download the project : you can download it as ZIP file or through git as following
    ```cmd
    git clone https://github.com/ibram-reda/AVREmulator.git
    ```
2. Open the project folder 
    ```
    cd .\AVREmulator
    ```
3. Run the program 

    - Restore Pakages
        ```
        dotnet restore
        ```
    - build the app
        ```
        dotnet Build --no-restore
        ```
    - run the UI
        ```
        dotnet run --project .\AVREmulator.UI\
        ```

**Note that**: the emulator is still not support all kind of AVR instructions so it will probably fail to execute the hex file. you can see all [supported instruction in our emulator][3]

the following is a simple code example can run and test with our emulator
```asm
Start:
    LDi r18,0x29  ;Load Register 18 with value 0x29 
    rjmp Start    ;Repete that again 
```
the hex file for the above program can be found [Here][4]


[1]: http://lyons42.com/AVR/Opcodes/AVRAllOpcodes.html#Block48
[2]: http://ww1.microchip.com/downloads/en/devicedoc/atmel-0856-avr-instruction-set-manual.pdf
[3]: https://github.com/ibram-reda/AVREmulator/tree/master/Docs#cpu--instruction
[4]: https://github.com/ibram-reda/AVREmulator/blob/master/AVREmulatorTests/AVRTestProgram/atmelTest.hex
[5]: https://dotnet.microsoft.com/en-us/download/dotnet/7.0
