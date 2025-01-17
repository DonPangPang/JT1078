﻿using System;
using System.Runtime.InteropServices;

namespace JT1078.Protocol.Extensions
{
    public static class InteropExtensions
    {
        public static T BytesToStruct<T>(byte[] bytes, int startIndex, int length)
        {
            T local;
            T local2;
            if (bytes == null)
            {
                local2 = default;
                local = local2;
            }
            else if (bytes.Length <= 0)
            {
                local2 = default;
                local = local2;
            }
            else
            {
                IntPtr destination = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.Copy(bytes, startIndex, destination, length);
                    local = (T)Marshal.PtrToStructure(destination, typeof(T));
                }
                catch (Exception exception)
                {
                    throw new Exception("Error in BytesToStruct ! " + exception.Message);
                }
                finally
                {
                    Marshal.FreeHGlobal(destination);
                }
            }
            return local;
        }

        public static void IntPtrSetValue(IntPtr intptr, object structObj)
        {
            IntPtrSetValue(intptr, StructToBytes(structObj));
        }

        public static void IntPtrSetValue(IntPtr intptr, byte[] bytes)
        {
            Marshal.Copy(bytes, 0, intptr, bytes.Length);
        }

        public static T IntPtrToStruct<T>(IntPtr intptr)
        {
            int index = 0;
            return IntPtrToStruct<T>(intptr, index, Marshal.SizeOf(typeof(T)));
        }

        public static T IntPtrToStruct<T>(IntPtr intptr, int index, int length)
        {
            byte[] destination = new byte[length];
            Marshal.Copy(intptr, destination, index, length);
            return BytesToStruct<T>(destination, 0, destination.Length);
        }

        public static byte[] StructToBytes(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            return StructToBytes(structObj, size);
        }

        public static byte[] StructToBytes(object structObj, int size)
        {
            byte[] buffer2;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structObj, ptr, false);
                byte[] destination = new byte[size];
                Marshal.Copy(ptr, destination, 0, size);
                buffer2 = destination;
            }
            catch (Exception exception)
            {
                throw new Exception("Error in StructToBytes ! " + exception.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return buffer2;
        }

        /// <summary>
        /// </summary>
        /// <param name="input"> </param>
        /// <returns> </returns>
        public static IntPtr ToIntPtr(this byte[] input)
        {
            int size = input.Length;
            if (size == 0) return IntPtr.Zero;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(input, 0, buffer, size);

                return buffer;
            }
            catch
            {
                Marshal.FreeHGlobal(buffer);
            }

            return IntPtr.Zero;
        }

        public static byte[] ToBytes(this IntPtr buffer, int bufferSize)
        {
            byte[] res = new byte[bufferSize];
            Marshal.Copy(buffer, res, 0, bufferSize);

            return res;
        }
    }
}