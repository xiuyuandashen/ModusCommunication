using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace ModusCommunication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //ModuleHandle moduleHandle = new ModuleHandle();   

            //// NuGet 安装 NModbus4 库
            //// 确定通讯方式 这边是串口
            //SerialPort port = new SerialPort("COM2");
            //// 波特率
            //port.BaudRate = 9600;
            //// 数据位
            //port.DataBits= 8;
            //// 停止位
            //port.StopBits = StopBits.One;
            //// 校验位
            //port.Parity = Parity.None;

            //port.Open();

            //var master = ModbusSerialMaster.CreateRtu(port);
            ////  读取保持型寄存器  slaveid 寄存器起始位置 读取个数
            //ushort [] values = master.ReadHoldingRegisters(1,10,2);
            //Console.WriteLine($"index 10:{values[0]},index 11:{values[1]}");
            //// 写入保持型寄存器  slaveid 在18号寄存器 写入 133
            //master.WriteSingleRegister(1, 18, 133);

            //// 读写线圈型寄存器 
            //bool [] coils = master.ReadCoils(2, 0, 3);
            //Console.WriteLine($"index 0:{coils[0]},index 2:{coils[2]}");
            //// 写入线圈状态 将2号线圈寄存器值改为false
            //master.WriteSingleCoil(2, 2, false);

            //// 在10寄存器处写入浮点数（实际占用10，11两个寄存器）
            //master.WriteFloat(1, 10, 3.14f);
            //Console.WriteLine($"写入浮点数:3.14");

            //// 读取浮点数  浮点数在Modbus中是由两位寄存器构成（一位寄存器是16bit）
            //float val = master.ReadFloat(1, 10, 2);
            //Console.WriteLine($"浮点数 index 10-11:{val}");
            //Console.ReadLine();
            //port.Close();

            ModbusTCPTest();
            Console.ReadLine();
        }

        static async void ModbusTCPTest()
        {
            ModbusTcp.ModbusClient client = new ModbusTcp.ModbusClient("127.0.0.1", 502);
            client.Init();
            short[] values = await client.ReadRegistersAsync(15,1);
            Console.WriteLine($"index 15:{values[0]}");
            float[] value = await client.ReadRegistersFloatsAsync(10, 4);
            Console.WriteLine($"index 10-11:{value[0]},index 12-13:{value[1]}");
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
