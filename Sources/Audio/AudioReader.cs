using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;

namespace iSpyApplication.Sources.Audio
{
    public unsafe class AudioReader : IDisposable
    {
        private const int BUFSIZE = 2000000;
        private const int TIMEOUT = 20;

        public readonly int rate = 22050;
        public readonly int channels = 1;

        private int _audioStreamIndex = -1;
        private AVFormatContext* _pFormatContext = null;
        private AVCodecContext* _pAudioCodecContext = null;
        private SwrContext* _pSwrContext = null;

        private readonly List<Filter> _filtersAudio = new List<Filter>();
        private readonly AVFilterContext* _pBufferSrcContext = null;
        private readonly AVFilterContext* _pBufferSinkContext = null;
        private AVFilterGraph* _pFilterGraph = null;
        private bool _disposed;

        public AudioReader()
        {
        }

        public AudioReader(int rate, int channels) : this()
        {
            this.rate = rate;
            this.channels = channels;
        }

        public List<Filter> AudioFilters => _filtersAudio;

        public void AddAudioFilter(string name, string args, string key = "")
        {
            _filtersAudio.Add(new Filter(name, args, key));
        }

        private AVFilterGraph* init_filter_graph(AVFormatContext* format, AVCodecContext* codec, AVFilterContext** buffersrc_ctx, AVFilterContext** buffersink_ctx)
        {
            var filter_graph = ffmpeg.avfilter_graph_alloc();

            var abuffersrc = ffmpeg.avfilter_get_by_name("abuffer");

            var chLayout = codec->ch_layout; // FIX: Correct variable name from 'varchLayout' to 'chLayout'
            var buf = stackalloc byte[128];
            ffmpeg.av_channel_layout_describe(&chLayout, buf, 128);
            var layoutString = Marshal.PtrToStringAnsi((IntPtr)buf);

            var args = $"sample_fmt={ffmpeg.av_get_sample_fmt_name(codec->sample_fmt)}:channel_layout={layoutString}:sample_rate={codec->sample_rate}:time_base={codec->time_base.num}/{codec->time_base.den}";

            int ret = ffmpeg.avfilter_graph_create_filter(buffersrc_ctx, abuffersrc, "IN", args, null, filter_graph);
            if (ret < 0) throw new ApplicationException("Failed to create abuffer filter.");

            var abuffersink = ffmpeg.avfilter_get_by_name("abuffersink");
            ret = ffmpeg.avfilter_graph_create_filter(buffersink_ctx, abuffersink, "OUT", "", null, filter_graph);
            if (ret < 0) throw new ApplicationException("Failed to create abuffersink filter.");

            AVFilterContext* prev_filter_ctx = *buffersrc_ctx;
            for (var i = 0; i < _filtersAudio.Count; i++)
            {
                var filter = ffmpeg.avfilter_get_by_name(_filtersAudio[i].name);
                AVFilterContext* filter_ctx;
                ffmpeg.avfilter_graph_create_filter(&filter_ctx, filter, $"{_filtersAudio[i].name}{_filtersAudio[i].key}".ToUpper(), _filtersAudio[i].args, null, filter_graph);

                ffmpeg.avfilter_link(prev_filter_ctx, 0, filter_ctx, 0);
                prev_filter_ctx = filter_ctx;

                if (i == _filtersAudio.Count - 1)
                {
                    ffmpeg.avfilter_link(prev_filter_ctx, 0, *buffersink_ctx, 0);
                }
            }
            if (_filtersAudio.Count == 0)
            {
                ffmpeg.avfilter_link(*buffersrc_ctx, 0, *buffersink_ctx, 0);
            }

            ffmpeg.avfilter_graph_config(filter_graph, null);

            return filter_graph;
        }

        private void open_input(string path)
        {
            try
            {
                var _timeoutMicroSeconds = Math.Max(5000000, TIMEOUT * 1000);

                AVDictionary* options = null;
                if (path.Contains(":"))
                {
                    var prefix = path.ToLower().Substring(0, path.IndexOf(":", StringComparison.Ordinal));
                    ffmpeg.av_dict_set_int(&options, "rw_timeout", _timeoutMicroSeconds, 0);
                    ffmpeg.av_dict_set_int(&options, "tcp_nodelay", 1, 0);
                    switch (prefix)
                    {
                        case "https":
                        case "http":
                        case "mmsh":
                        case "mms":
                        case "rtsp":
                        case "rtmp":
                            ffmpeg.av_dict_set_int(&options, "stimeout", _timeoutMicroSeconds, 0);
                            if (prefix == "rtsp")
                                ffmpeg.av_dict_set(&options, "rtsp_flags", "prefer_tcp", 0);
                            break;
                        default:
                            ffmpeg.av_dict_set_int(&options, "timeout", _timeoutMicroSeconds, 0);
                            break;
                    }
                    ffmpeg.av_dict_set_int(&options, "buffer_size", BUFSIZE, 0);
                }

                _pFormatContext = ffmpeg.avformat_alloc_context();
                _pFormatContext->max_analyze_duration = 0;

                int ret;
                fixed (AVFormatContext** fmt_ctx = &_pFormatContext)
                {
                    ret = ffmpeg.avformat_open_input(fmt_ctx, path, null, &options);
                }

                if (ret < 0) throw new ApplicationException("Failed to open input.");

                ret = ffmpeg.avformat_find_stream_info(_pFormatContext, null);
                if (ret < 0) throw new ApplicationException("Failed to find stream information.");

                AVCodec* dec;
                ret = ffmpeg.av_find_best_stream(_pFormatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &dec, 0);
                if (ret < 0) throw new ApplicationException("Failed to find an audio stream in input.");

                _audioStreamIndex = ret;
                var pStream = _pFormatContext->streams[_audioStreamIndex];

                _pFormatContext->flags |= ffmpeg.AVFMT_FLAG_DISCARD_CORRUPT | ffmpeg.AVFMT_FLAG_NOBUFFER;

                var pCodecParams = pStream->codecpar;
                dec = ffmpeg.avcodec_find_decoder(pCodecParams->codec_id);
                if (dec == null) throw new ApplicationException("Unsupported audio codec.");

                _pAudioCodecContext = ffmpeg.avcodec_alloc_context3(dec);
                if (_pAudioCodecContext == null) throw new ApplicationException("Could not allocate audio codec context.");

                ret = ffmpeg.avcodec_parameters_to_context(_pAudioCodecContext, pCodecParams);
                if (ret < 0) throw new ApplicationException("Could not copy codec parameters to context.");

                if ((ret = ffmpeg.avcodec_open2(_pAudioCodecContext, dec, null)) < 0)
                {
                    throw new ApplicationException("Failed to open audio decoder.");
                }

                if (_filtersAudio.Count > 0)
                {
                    fixed (AVFilterContext** bsrc = &_pBufferSrcContext, bsink = &_pBufferSinkContext)
                    {
                        _pFilterGraph = init_filter_graph(_pFormatContext, _pAudioCodecContext, bsrc, bsink);
                        if (_pFilterGraph == null) throw new ApplicationException("Failed to create the filter graph.");
                    }
                }

                AVChannelLayout out_ch_layout;
                ffmpeg.av_channel_layout_default(&out_ch_layout, channels);

                fixed (SwrContext** swrContext = &_pSwrContext)
                {
                    ffmpeg.swr_alloc_set_opts2(swrContext,
                        &out_ch_layout, AVSampleFormat.AV_SAMPLE_FMT_S16, rate,
                        &_pAudioCodecContext->ch_layout, _pAudioCodecContext->sample_fmt, _pAudioCodecContext->sample_rate,
                        0, null);
                }
                ffmpeg.av_channel_layout_uninit(&out_ch_layout);

                if (_pSwrContext == null) throw new ApplicationException("Failed to create SwrContext");

                ret = ffmpeg.swr_init(_pSwrContext);
                if (ret < 0) throw new ApplicationException("Failed to initialize SwrContext.");
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public void ReadSamples(string inputAudio, Func<byte[], int, bool> readSampleCallback)
        {
            if (readSampleCallback == null) return;
            try
            {
                open_input(inputAudio);

                var brk = false;
                AVPacket* packet = ffmpeg.av_packet_alloc();
                AVFrame* pFrame = ffmpeg.av_frame_alloc();
                AVFrame* pFilteredFrame = ffmpeg.av_frame_alloc();

                if (packet == null || pFrame == null || pFilteredFrame == null)
                    throw new OutOfMemoryException("Failed to allocate FFmpeg packet or frames.");

                try
                {
                    byte[] outBuffer = new byte[rate * 2 * channels]; // 1 second buffer
                    byte[] resampleBuffer = new byte[rate * 2 * channels];

                    while (!brk)
                    {
                        var ret = ffmpeg.av_read_frame(_pFormatContext, packet);
                        if (ret < 0)
                        {
                            break;
                        }

                        if ((packet->flags & ffmpeg.AV_PKT_FLAG_CORRUPT) != 0)
                        {
                            continue;
                        }

                        if (packet->stream_index == _audioStreamIndex)
                        {
                            ret = ffmpeg.avcodec_send_packet(_pAudioCodecContext, packet);
                            if (ret < 0) continue;

                            int samples_written = 0;

                            while (ret >= 0)
                            {
                                ret = ffmpeg.avcodec_receive_frame(_pAudioCodecContext, pFrame);
                                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                                {
                                    break;
                                }
                                if (ret < 0)
                                {
                                    brk = true;
                                    break;
                                }

                                AVFrame* pProcessFrame = pFrame;

                                if (_pFilterGraph != null)
                                {
                                    if (ffmpeg.av_buffersrc_add_frame_flags(_pBufferSrcContext, pFrame, 0) < 0) break;

                                    if (ffmpeg.av_buffersink_get_frame(_pBufferSinkContext, pFilteredFrame) < 0) continue;
                                    pProcessFrame = pFilteredFrame;
                                }

                                fixed (byte* pResampleBuffer = &resampleBuffer[0])
                                {
                                    byte** pDest = &pResampleBuffer;
                                    var numSamplesOut = ffmpeg.swr_convert(_pSwrContext, pDest, pProcessFrame->nb_samples, pProcessFrame->extended_data, pProcessFrame->nb_samples);

                                    if (numSamplesOut > 0)
                                    {
                                        var byteCount = numSamplesOut * channels * ffmpeg.av_get_bytes_per_sample(AVSampleFormat.AV_SAMPLE_FMT_S16);
                                        Buffer.BlockCopy(resampleBuffer, 0, outBuffer, samples_written, byteCount);
                                        samples_written += byteCount;
                                    }
                                }
                                ffmpeg.av_frame_unref(pFilteredFrame);
                            }

                            if (samples_written > 0)
                            {
                                var finalBuffer = new byte[samples_written];
                                Buffer.BlockCopy(outBuffer, 0, finalBuffer, 0, samples_written);
                                if (readSampleCallback(finalBuffer, samples_written))
                                {
                                    brk = true;
                                }
                            }
                        }
                        ffmpeg.av_packet_unref(packet);

                        if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                        {
                            Thread.Sleep(10);
                        }
                    }
                }
                finally
                {
                    ffmpeg.av_frame_free(&pFrame);
                    ffmpeg.av_frame_free(&pFilteredFrame);
                    ffmpeg.av_packet_free(&packet);
                }
            }
            finally
            {
                Dispose();
            }
        }

        ~AudioReader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (_pAudioCodecContext != null)
            {
                fixed (AVCodecContext** c = &_pAudioCodecContext) ffmpeg.avcodec_free_context(c);
            }
            _pAudioCodecContext = null;

            if (_pFormatContext != null)
            {
                fixed (AVFormatContext** c = &_pFormatContext) ffmpeg.avformat_close_input(c);
            }
            _pFormatContext = null;

            if (_pSwrContext != null)
            {
                fixed (SwrContext** s = &_pSwrContext) ffmpeg.swr_free(s);
                _pSwrContext = null;
            }

            if (_pFilterGraph != null)
            {
                fixed (AVFilterGraph** f = &_pFilterGraph) ffmpeg.avfilter_graph_free(f);
                _pFilterGraph = null;
            }

            if (disposing)
            {
                _filtersAudio.Clear();
            }

            _disposed = true;
        }

        public class Filter
        {
            public readonly string name;
            public readonly string args;
            public readonly string key;

            public Filter(string name, string args, string key = "")
            {
                this.name = name;
                this.args = args;
                this.key = key;
            }
        }
    }
}