﻿using JT1078.FMp4.Enums;
using JT1078.FMp4.MessagePack;
using JT1078.FMp4.Samples;
using JT1078.Protocol;
using JT1078.Protocol.Enums;
using JT1078.Protocol.H264;
using JT1078.Protocol.MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JT1078.FMp4
{
    /// <summary>
    /// FMp4编码
    /// fmp4
    /// stream data 
    /// ftyp
    /// moov
    /// styp 1
    /// moof 1
    /// mdat 1
    /// ...
    /// styp n
    /// moof n
    /// mdat n
    /// mfra
    /// ref: https://www.w3.org/TR/mse-byte-stream-format-isobmff/#movie-fragment-relative-addressing
    /// </summary>
    public class FMp4Encoder
    {
        /// <summary>
        /// 编码ftyp盒子
        /// </summary>
        /// <returns></returns>
        public byte[] EncoderFtypBox()
        {
            byte[] buffer = FMp4ArrayPool.Rent(1024);
            FMp4MessagePackWriter writer = new FMp4MessagePackWriter(buffer);
            try
            {
                //ftyp
                //FileTypeBox fileTypeBox = new FileTypeBox();
                //fileTypeBox.MajorBrand = "isom";
                //fileTypeBox.MinorVersion = "\0\0\u0002\0";
                //fileTypeBox.CompatibleBrands.Add("isom");
                //fileTypeBox.CompatibleBrands.Add("iso2");
                //fileTypeBox.CompatibleBrands.Add("avc1");
                //fileTypeBox.CompatibleBrands.Add("mp41");
                //fileTypeBox.CompatibleBrands.Add("iso5");
                FileTypeBox fileTypeBox = new FileTypeBox();
                fileTypeBox.MajorBrand = "msdh";
                fileTypeBox.MinorVersion = "\0\0\0\0";
                fileTypeBox.CompatibleBrands.Add("isom");
                fileTypeBox.CompatibleBrands.Add("mp42");
                fileTypeBox.CompatibleBrands.Add("msdh");
                fileTypeBox.CompatibleBrands.Add("msix");
                fileTypeBox.CompatibleBrands.Add("iso5");
                fileTypeBox.CompatibleBrands.Add("iso6");
                fileTypeBox.ToBuffer(ref writer);
                var data = writer.FlushAndGetArray();
                return data;
            }
            finally
            {
                FMp4ArrayPool.Return(buffer);
            }
        }

        /// <summary>
        /// 编码moov盒子
        /// </summary>
        /// <returns></returns>
        public byte[] EncoderMoovBox(in H264NALU sps, in H264NALU pps)
        {
            byte[] buffer = FMp4ArrayPool.Rent(sps.RawData.Length+ pps.RawData.Length  + 1024);
            FMp4MessagePackWriter writer = new FMp4MessagePackWriter(buffer);
            try
            {
                ExpGolombReader h264GolombReader = new ExpGolombReader(sps.RawData);
                var spsInfo = h264GolombReader.ReadSPS();
                //moov
                MovieBox movieBox = new MovieBox();
                movieBox.MovieHeaderBox = new MovieHeaderBox(0, 2);
                movieBox.MovieHeaderBox.CreationTime = 0;
                movieBox.MovieHeaderBox.ModificationTime = 0;
                movieBox.MovieHeaderBox.Duration = 0;
                movieBox.MovieHeaderBox.Timescale = 1000;
                movieBox.MovieHeaderBox.NextTrackID = 99;
                movieBox.TrackBox = new TrackBox();
                movieBox.TrackBox.TrackHeaderBox = new TrackHeaderBox(0, 3);
                movieBox.TrackBox.TrackHeaderBox.CreationTime = 0;
                movieBox.TrackBox.TrackHeaderBox.ModificationTime = 0;
                movieBox.TrackBox.TrackHeaderBox.TrackID = 1;
                movieBox.TrackBox.TrackHeaderBox.Duration = 0;
                movieBox.TrackBox.TrackHeaderBox.TrackIsAudio = false;
                movieBox.TrackBox.TrackHeaderBox.Width = (uint)spsInfo.width;
                movieBox.TrackBox.TrackHeaderBox.Height = (uint)spsInfo.height;
                movieBox.TrackBox.MediaBox = new MediaBox();
                movieBox.TrackBox.MediaBox.MediaHeaderBox = new MediaHeaderBox();
                movieBox.TrackBox.MediaBox.MediaHeaderBox.CreationTime = 0;
                movieBox.TrackBox.MediaBox.MediaHeaderBox.ModificationTime = 0;
                movieBox.TrackBox.MediaBox.MediaHeaderBox.Timescale = 1200000;
                movieBox.TrackBox.MediaBox.MediaHeaderBox.Duration = 0;
                movieBox.TrackBox.MediaBox.HandlerBox = new HandlerBox();
                movieBox.TrackBox.MediaBox.HandlerBox.HandlerType = HandlerType.vide;
                movieBox.TrackBox.MediaBox.HandlerBox.Name = "VideoHandler";
                movieBox.TrackBox.MediaBox.MediaInformationBox = new MediaInformationBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.VideoMediaHeaderBox = new VideoMediaHeaderBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.DataInformationBox = new DataInformationBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.DataInformationBox.DataReferenceBox = new DataReferenceBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.DataInformationBox.DataReferenceBox.DataEntryBoxes = new List<DataEntryBox>();
                movieBox.TrackBox.MediaBox.MediaInformationBox.DataInformationBox.DataReferenceBox.DataEntryBoxes.Add(new DataEntryUrlBox(1));
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox = new SampleTableBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionBox = new SampleDescriptionBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionBox.SampleEntries = new List<SampleEntry>();
                AVC1SampleEntry avc1 = new AVC1SampleEntry();
                avc1.AVCConfigurationBox = new AVCConfigurationBox();
                //h264
                avc1.Width = (ushort)movieBox.TrackBox.TrackHeaderBox.Width;
                avc1.Height = (ushort)movieBox.TrackBox.TrackHeaderBox.Height;
                avc1.AVCConfigurationBox.AVCLevelIndication = spsInfo.levelIdc;
                avc1.AVCConfigurationBox.AVCProfileIndication = spsInfo.profileIdc;
                avc1.AVCConfigurationBox.ProfileCompatibility = (byte)spsInfo.profileCompat;
                avc1.AVCConfigurationBox.PPSs = new List<byte[]>() { pps.RawData };
                avc1.AVCConfigurationBox.SPSs = new List<byte[]>() { sps.RawData };
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionBox.SampleEntries.Add(avc1);
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.TimeToSampleBox = new TimeToSampleBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SyncSampleBox = new SyncSampleBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleToChunkBox = new SampleToChunkBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleSizeBox = new SampleSizeBox();
                movieBox.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.ChunkOffsetBox = new ChunkOffsetBox();
                movieBox.MovieExtendsBox = new MovieExtendsBox();
                movieBox.MovieExtendsBox.TrackExtendsBoxs = new List<TrackExtendsBox>();
                TrackExtendsBox trex = new TrackExtendsBox();
                trex.TrackID = 1;
                trex.DefaultSampleDescriptionIndex = 1;
                trex.DefaultSampleDuration = 0;
                trex.DefaultSampleSize = 0;
                trex.DefaultSampleFlags = 0;
                movieBox.MovieExtendsBox.TrackExtendsBoxs.Add(trex);
                movieBox.ToBuffer(ref writer);
                var data = writer.FlushAndGetArray();
                return data;
            }
            finally
            {
                FMp4ArrayPool.Return(buffer);
            }
        }

        uint sn = 1;

        public ulong timestampCache = 0;

        /// <summary>
        /// 编码其他视频数据盒子
        /// </summary>
        /// <returns></returns>
        public byte[] EncoderOtherVideoBox(List<H264NALU> nalus)
        {
            byte[] buffer = FMp4ArrayPool.Rent(nalus.Sum(s => s.RawData.Length + s.StartCodePrefix.Length) + 4096);
            FMp4MessagePackWriter writer = new FMp4MessagePackWriter(buffer);
            try
            {
                SegmentTypeBox stypTypeBox = new SegmentTypeBox();
                stypTypeBox.MajorBrand = "msdh";
                stypTypeBox.MinorVersion = "\0\0\0\0";
                stypTypeBox.CompatibleBrands.Add("isom");
                stypTypeBox.CompatibleBrands.Add("mp42");
                stypTypeBox.CompatibleBrands.Add("msdh");
                stypTypeBox.CompatibleBrands.Add("msix");
                stypTypeBox.CompatibleBrands.Add("iso5");
                stypTypeBox.CompatibleBrands.Add("iso6");
                stypTypeBox.ToBuffer(ref writer);

                var firstNalu = nalus[0];
                var lastNalu = nalus[nalus.Count - 1];
                uint interval = (uint)(lastNalu.Timestamp - firstNalu.Timestamp);
                int iSize = nalus.Where(w => w.DataType == Protocol.Enums.JT1078DataType.视频I帧)
                    .Sum(s => s.RawData.Length + s.StartCodePrefix.Length);

                List<int> sizes = new List<int>();
                sizes.Add(iSize);
                sizes = sizes.Concat(nalus.Where(w => w.DataType != Protocol.Enums.JT1078DataType.视频I帧)
                            .Select(s => s.RawData.Length + s.StartCodePrefix.Length).ToList())
                            .ToList();
                SegmentIndexBox segmentIndexBox = new SegmentIndexBox(1);
                segmentIndexBox.ReferenceID = 1;
                segmentIndexBox.EarliestPresentationTime = timestampCache;
                segmentIndexBox.SegmentIndexs = new List<SegmentIndexBox.SegmentIndex>()
                {
                     new SegmentIndexBox.SegmentIndex
                     {
                          SubsegmentDuration=interval
                     }
                };
                segmentIndexBox.ToBuffer(ref writer);

                var current1 = writer.GetCurrentPosition();

                var movieFragmentBox = new MovieFragmentBox();
                movieFragmentBox.MovieFragmentHeaderBox = new MovieFragmentHeaderBox();
                movieFragmentBox.MovieFragmentHeaderBox.SequenceNumber = sn++;
                movieFragmentBox.TrackFragmentBox = new TrackFragmentBox();
                movieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox = new TrackFragmentHeaderBox(0x2003a);
                movieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.TrackID = 1;
                movieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.SampleDescriptionIndex = 1;
                movieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.DefaultSampleDuration = 48000;
                movieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.DefaultSampleSize = (uint)iSize;
                movieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.DefaultSampleFlags = 0x1010000;
                movieFragmentBox.TrackFragmentBox.TrackFragmentBaseMediaDecodeTimeBox = new TrackFragmentBaseMediaDecodeTimeBox();
                movieFragmentBox.TrackFragmentBox.TrackFragmentBaseMediaDecodeTimeBox.BaseMediaDecodeTime = timestampCache;

                //trun
                movieFragmentBox.TrackFragmentBox.TrackRunBox = new TrackRunBox(flags: 0x205);
                movieFragmentBox.TrackFragmentBox.TrackRunBox.FirstSampleFlags = 33554432;
                movieFragmentBox.TrackFragmentBox.TrackRunBox.TrackRunInfos = new List<TrackRunBox.TrackRunInfo>();

                foreach (var size in sizes)
                {
                    movieFragmentBox.TrackFragmentBox.TrackRunBox.TrackRunInfos.Add(new TrackRunBox.TrackRunInfo()
                    {
                        SampleSize = (uint)size,
                    });
                }

                movieFragmentBox.ToBuffer(ref writer);
                timestampCache += (uint)(sizes.Count * 48000);

                var mediaDataBox = new MediaDataBox();
                mediaDataBox.Data = nalus.Select(s => s.RawData).ToList();
                mediaDataBox.ToBuffer(ref writer);

                var current2 = writer.GetCurrentPosition();
                foreach(var postion in segmentIndexBox.ReferencedSizePositions)
                {
                    writer.WriteUInt32Return((uint)(current2 - current1), postion);
                }      
                var data = writer.FlushAndGetArray();
                return data;
            }
            finally
            {
                FMp4ArrayPool.Return(buffer);
            }
        }
    }
}
