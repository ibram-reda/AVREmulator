## Memory Spaces.
AVR uses Harvard architecture, which means that there are seperate buses for the code and the data memory

ther is two kind of memory space in avr 
1. Code memory spcae
2. Data memory space


![ATmega Memory Map(b)](https://user-images.githubusercontent.com/37075700/169544973-80428c95-7afb-49ec-8ad8-4360ff9598e6.gif)

### Data memory Space
it consist of three sections 
1. **General Purpose Registers (GPRs)**: use the first 32 bytes of data memory space they take location 0x00-0x1f
2. **I/O Memory (SFRs spacial function registers)**: the I/O memory dedicated to spacific function s such as status register,timers, I/O ports, ADC and so on. All AVR chips has at least 64 byte of IO memory called stander IO memory ... it could have more IO memory and it will be called extendend IO memory
3. **Internal Data SRAM** 



![DataMemorySpace](https://user-images.githubusercontent.com/37075700/169546472-7f652e03-4373-4dcd-8f9e-6f1bb7c3f157.PNG)


### Code Memory space (ROM)
is alwase a two byte wide memory and it's the place that the program counter point to

at powering up PC is point to location 0X0000 in this ROM

All instraction in AVR is only 2byte or 4 byte wide ther is no 2byte or 3 byte