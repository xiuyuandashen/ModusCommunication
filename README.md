

# Modbus
> modbus使用范围广泛，广泛应用于各类仪表，PLC等。它属于应用层协议，底层硬件基于485/以太网。


Modbus的存储区有：输入线圈（布尔，只读，代号1区），输入寄存器（寄存器，只读，代号3区），输出线圈（读写，布尔，代号0区），输出寄存器（寄存器，读写，代号4区）

Modbus是典型的半双工模式，有且只有一个master，请求由master发出，slave响应。slave之间不能通讯，只能通过master转达。相当于master是客户端，slave都是服务器。

## Modbus模拟工具
模拟工具使用Modbus Slave 以及 Modbus Poll 。其中 Slave相当于服务器（Modbus Slave），Poll相当于客户端（Modbus Master）。

### 模拟工具使用

#### 配置Slave
配置Slave
![在这里插入图片描述](https://img-blog.csdnimg.cn/9c8e1fea05764bf09571493c1320f1a3.png)
基本配置，配置完选择ok，接下来只要配置要使用的接口方式（网卡，串口等）

![在这里插入图片描述](https://img-blog.csdnimg.cn/f4b91d6a643b491abf9cf2478b61fc32.png)
![在这里插入图片描述](https://img-blog.csdnimg.cn/fd2e0c6998d64683ab73b148e4b5557f.png)
选择接口方式，选择串口，初始化波特率、数据位、校验位、停止位，然后选择ok即可打开链接。
![在这里插入图片描述](https://img-blog.csdnimg.cn/7fa6838d37b04e34a6d28e39cf518c17.png)

#### 配置Poll
打开Poll选择需要进行的操作
![在这里插入图片描述](https://img-blog.csdnimg.cn/97e1eb97fea54b8889bcdd2c10c63589.png)

选择写入可以寄存器，会发现Slave这边对应的已经改变了
![在这里插入图片描述](https://img-blog.csdnimg.cn/09e9a632a90e449cb49103fbefea8d20.png)
![在这里插入图片描述](https://img-blog.csdnimg.cn/dd93cff649a54b9da6bcae68721e6f99.png)



![在这里插入图片描述](https://img-blog.csdnimg.cn/68743a8e4264414fad98baa3581083f6.png)
上面type中可以写入浮点数等类型。

## C#使用ModBus通讯

使用NuGet中的`NModbus4`通讯库，进行ModBus RTU（串口）通讯


```csharp
namespace ModusCommunication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ModuleHandle moduleHandle = new ModuleHandle();   

            // NuGet 安装 NModbus4 库
            // 确定通讯方式 这边是串口
            SerialPort port = new SerialPort("COM2");
            // 波特率
            port.BaudRate = 9600;
            // 数据位
            port.DataBits= 8;
            // 停止位
            port.StopBits = StopBits.One;
            // 校验位
            port.Parity = Parity.None;

            port.Open();

            var master = ModbusSerialMaster.CreateRtu(port);
            //  读取保持型寄存器  slaveid 寄存器起始位置 读取个数
            ushort [] values = master.ReadHoldingRegisters(1,10,2);
            Console.WriteLine($"index 10:{values[0]},index 11:{values[1]}");
            // 写入保持型寄存器  slaveid 在18号寄存器 写入 133
            master.WriteSingleRegister(1, 18, 133);

            // 读写线圈型寄存器 
            bool [] coils = master.ReadCoils(2, 0, 3);
            Console.WriteLine($"index 0:{coils[0]},index 2:{coils[2]}");
            // 写入线圈状态 将2号线圈寄存器值改为false
            master.WriteSingleCoil(2, 2, false);

            // 在10寄存器处写入浮点数（实际占用10，11两个寄存器）
            master.WriteFloat(1, 10, 3.14f);
            Console.WriteLine($"写入浮点数:3.14");

            // 读取浮点数  浮点数在Modbus中是由两位寄存器构成（一位寄存器是16bit）
            float val = master.ReadFloat(1, 10, 2);
            Console.WriteLine($"浮点数 index 10-11:{val}");
            Console.ReadLine();
            port.Close();


        }

        
    }
}

public  static class NModbusExtensions
{
    /// <summary>
    /// 从寄存器中读取float值
    /// </summary>
    /// <param name="master"></param>
    /// <param name="slaveAddress"></param>
    /// <param name="startAddress"></param>
    /// <param name="numberOfPoints"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static float ReadFloat(this ModbusSerialMaster master , byte slaveAddress, ushort startAddress, ushort numberOfPoints)
    {
        try
        {
            float? floatValue;
            ushort[] registers = master.ReadHoldingRegisters(slaveAddress, startAddress, 2); // 2寄存器对应一个浮点数

            if (registers.Length == 2)
            {
                // 从两个16位整数重新组合为浮点数
                ushort intValue1 = registers[1]; // 低位
                ushort intValue2 = registers[0]; // 高位

                byte[] bytes = new byte[4];
                Buffer.BlockCopy(new ushort[] { intValue1, intValue2 }, 0, bytes, 0, 4);

                floatValue = BitConverter.ToSingle(bytes, 0);
                return floatValue.Value;
            }
            else
            {
                throw new Exception();
            }
        }catch(Exception ex)
        {
            throw new Exception("读取失败");
        }
        
    }

    /// <summary>
    /// 向寄存器中写入浮点数
    /// </summary>
    /// <param name="master"></param>
    /// <param name="slaveAddress"></param>
    /// <param name="startAddress"></param>
    /// <param name="value"></param>
    public static void WriteFloat(this ModbusSerialMaster master, byte slaveAddress, ushort startAddress, float value)
    {
        // 将浮点数转换为字节数组
        byte[] bytes = BitConverter.GetBytes(value);

        // 提取字节数组中的两个16位整数
        ushort intValue1 = BitConverter.ToUInt16(bytes, 0); // 低位
        ushort intValue2 = BitConverter.ToUInt16(bytes, 2); // 高位

        master.WriteMultipleRegisters(slaveAddress, startAddress, new ushort[] { intValue2, intValue1 });

    }

}
```


![在这里插入图片描述](https://img-blog.csdnimg.cn/4a18406b2a534e6596661847617a22ad.png)


## 在无法使用 SerialPort类 的情况下使用TCP进行

在Web以及移动端是不能使用SerialPort的，这时候可以使用TCP进行链接
在slave连接配置中是有ip地址以及端口的，可以通过这个进行ModbusTcp通讯
![在这里插入图片描述](https://img-blog.csdnimg.cn/afd62bff065048e9aab6c117b34c00d4.png)
> NuGet 安装 ModbusTcp 包

```csharp

static async void ModbusTCPTest()
{
    ModbusTcp.ModbusClient client = new ModbusTcp.ModbusClient("127.0.0.1", 502);
    client.Init();
    // 读取一个 整数
    short[] values = await client.ReadRegistersAsync(15,1);
    Console.WriteLine($"index 15:{values[0]}");
    // 读取两个浮点数
    float[] value = await client.ReadRegistersFloatsAsync(10, 4);
    Console.WriteLine($"index 10-11:{value[0]},index 12-13:{value[1]}");
}

```

![在这里插入图片描述](https://img-blog.csdnimg.cn/edff224097994d4c8e7515d6759c4a5a.png)