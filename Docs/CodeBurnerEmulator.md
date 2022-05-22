# Code Burner Emulator
The component which responsable To uploade AVR code into the Emulator flash memory .... 
it would suport a multible file format in future but for now it only suport Intel Hex File


## Intel Hex File Format
Each line in an Intel Hex file has the same basic layout, like this:
```
:BBAAAATT[DDDDDDDD]CC
```
where
- **:** is start of line marker
- **BB** is number of data bytes on line
- **AAAA** is address in bytes
- **TT** is type discussed below but 00 means data
- **DD** is data bytes, number depends on BB value
- **CC** is checksum (2s-complement of number of bytes+address+type+data), wich responsable to check that the line is Delivered correctly ... (used in error detection in coummunication channal)

Here is an example.
```
:10000000112233445566778899AABBCCDDEEFF00F8
```
The line must start with a colon, :, followed by the number of data bytes on the line, in this case 0x10 or 16 decimal. Each data byte is represented by 2 characters.

Here is an example with only four bytes of data on the line:
```
:040010001122334442
```
Next on the line comes a 2 byte address, represented by 4 characters, with possible values from 0x0000 to 0xFFFF (0 to 64KB). This is followed by the type. This can have a range of values depending on whether the line contains data or not. Type 00 like these examples shows that the line contains data.

The data bytes come next, 2 characters to a byte. The number of data bytes is set at the beginning of the line.

### line Type (TT)
this byte will indicate the type of line in the intel hex file
- **00** indicates that the line contains data bytes
- **01** indicates the Last line in the Hex file
- **02** show that the line is an extended address line.

#### Extended Addresses
Intel Hex file format can only have address range of 0x0000 to 0xFFFF,(46KB of data), Newer microcontrollers and certainly memory chips can hold much more data than this so how do we include data above 64KB?

The answer is to have a multible segments (Block) each is 46KB, and the absolute address will be `(segAddr << 4) + addr` , [stackoverflow disction][1]

for more information on HexFile formate revise section 8.3 in Book page no. 300;

[1]: https://stackoverflow.com/questions/20808020/how-do-you-understand-hex-file-extended-address-record