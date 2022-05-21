## CPU Registers

### Program Counter Registe (PC)
The most important register in the AVR microcontroller is The PC. The program counter is used by CPU to point to the address o the next instruction to be executed. 

program counter is incremented automatically to point to the next instruction.

The Wider the program counter , the more memory locations a CPU can access. the program counter width can be up to 22 bits wide


Controller       | PC size    |  Rom size               | code Address Range
-----------------|------------|-------------------------|----------------
Atiny 25         | 10 bit     | 1k   * 2byte   = 2kb    | 00000 - 003FF
Atmega 8         | 12 bit     | 4k   * 2bytes  = 8kb    | 00000 - 00FFF
atmega 32        | 14 bit     | 16K  * 2 bytes = 32kb   | 00000 - 03FFF
atmega 64        | 15 bit     | 32k  * 2 bytes = 64kb   | 00000 - 07FFF
Atmega 128       | 16 bit     | 64k  * 2bytes  = 1288kb | 00000 - 0FFFF
Atmega 256       | 17 bit     | 128k * 2bytes  = 256kb  | 00000 - 1FFFF
