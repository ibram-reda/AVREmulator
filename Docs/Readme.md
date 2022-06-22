# AVR Emulator Components
We will emulate each part of AVR controller separately. And the following is the basic and essential components
1. CPU
2. RAM
3. ROM
4. DATA Bus
5. Program Bus

After we finish of building this basic component, then we will add other components which represent peripheral devices such as Timer,ADC,USRT,I2C,SPI,...etc

The most important component in our emulator is CPU part wich is consist basiclly of three main functionallity
1. `FetchInstruction` get opcode from flash memory
2. `DecodeInstruction` take the opcode and understand it and return an Executable instruction for that opcode  
3. `ExecuteInstruction` Execute an executable instraction and return how many cycle this instruction consumed

## CPU  instruction 
here is samury of supported instruction (check :heavy_check_mark: means that is alredy implemented in our emulator)

<table >
<thead>
  <tr>
    <th></th>
    <th>x0zz</th>
    <th>x1zz</th>
    <th>x2zz</th>
    <th>x3zz</th>
    <th>x4zz</th>
    <th>x5zz</th>
    <th>x6zz</th>
    <th>x7zz</th>
    <th>x8zz</th>
    <th>x9zz</th>
    <th>xAzz</th>
    <th>xBzz</th>
    <th>xCzz</th>
    <th>xDzz</th>
    <th>xEzz</th>
    <th>xFzz</th>
  </tr>
</thead>
<tbody align="center">
  <tr>
    <td>0xzz</td>
    <td>255 [R], nop :heavy_check_mark:</td>
    <td>movw :heavy_check_mark:</td>
    <td>muls</td>
    <td>fmul, fmuls, fmulsu, mulsu</td>
    <td colspan="4">cpc</td>    
    <td colspan="4">sbc</td>
    <td colspan="4">add</td>
  </tr>
  <tr>
    <td>1xzz</td>
    <td colspan="4" >cpse</td>
    <td colspan="4" >cp</td>
    <td colspan="4" >sub</td>
    <td colspan="4" >adc</td>
  </tr>
  <tr>
    <td>2xzz</td>
    <td colspan="4">and</td>
    <td colspan="4">eor</td>
    <td colspan="4">or</td>
    <td colspan="4">mov :heavy_check_mark:</td>
  </tr>
  <tr>
    <td>3xzz</td>
    <td colspan="16">cpi</td>
  </tr>
  <tr>
    <td>4xzz</td>
    <td colspan="16">sbci</td>
  </tr>
  <tr>
    <td>5xzz</td>
    <td colspan="16">subi</td>
  </tr>
  <tr>
    <td>6xzz</td>
    <td colspan="16">ori</td>
  </tr>
  <tr>
    <td>7xzz</td>
    <td colspan="16">andi</td>
  </tr>
  <tr>
    <td>8xzz</td>
    <td colspan="2">32 ld, 224 ldd</td>
    <td colspan="2">32 st, 224 std</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
  </tr>
  <tr>
    <td>9xzz</td>
    <td colspan="2">(MISC) :heavy_check_mark:</td>
    <td colspan="2">112 [R], 16 push, 112 st, 16 sts</td>
    <td>(MISC)</td>
    <td>(MISC)</td>
    <td>adiw</td>
    <td>sbiw</td>
    <td>cbi</td>
    <td>sbic</td>
    <td>sbi</td>
    <td>sbis</td>
    <td colspan="4">mul</td>
  </tr>
  <tr>
    <td>Axzz</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
    <td colspan="2">ldd</td>
    <td colspan="2">std</td>
  </tr>
  <tr>
    <td>Bxzz</td>
    <td colspan="8">in</td>
    <td colspan="8">out</td>
  </tr>
  <tr>
    <td>Cxzz</td>
    <td colspan="16">rjmp :heavy_check_mark:</td>
  </tr>
  <tr>
    <td>Dxzz</td>
    <td colspan="16">rcall</td>
  </tr>
  <tr>
    <td>Exzz</td>
    <td colspan="16">ldi :heavy_check_mark:</td>
  </tr>
  <tr>
    <td>Fxzz</td>
    <td colspan="4">Conditional Branches</td>
    <td colspan="4">Conditional Branches</td>
    <td colspan="2">[R], bld</td>
    <td colspan="2">[R], bst</td>
    <td colspan="2">[R], sbrc</td>
    <td colspan="2">[R], sbrs</td>
  </tr>  
</tbody>
</table>

you can also see `Appendx A: AVR Instructions Explained` in the Book page 695
