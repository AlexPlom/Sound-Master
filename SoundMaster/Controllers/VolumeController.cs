using Microsoft.AspNetCore.Mvc;
using NAudio.Lame;
using NAudio.Wave;

namespace SoundMaster.Controllers
{
    [Route("Volume")]
    public class VolumeController : ApiControllerBase
    {
        private readonly ILogger<VolumeController> _logger;

        public VolumeController(ILogger<VolumeController> logger)
        {
            _logger = logger;
        }

        [HttpPost, Route("lower-volume")]
        public IActionResult LowerVolume([FromForm] IFormFile file, float volumeToAdjustTo)
        {
            if (file == null)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var mp3Stream = file.OpenReadStream())
                using (var mp3Reader = new Mp3FileReader(mp3Stream))
                using (var waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader))
                {
                    var volumeProvider = new VolumeWaveProvider16(waveStream) { Volume = volumeToAdjustTo };
                    var outPath = Path.GetTempFileName();
                    using (var writer = new LameMP3FileWriter(outPath, volumeProvider.WaveFormat, LAMEPreset.ABR_320))
                    {
                        byte[] buffer = new byte[waveStream.WaveFormat.AverageBytesPerSecond];
                        int bytesRead;
                        while ((bytesRead = volumeProvider.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            writer.Write(buffer, 0, bytesRead);
                        }
                    }

                    var outputBytes = System.IO.File.ReadAllBytes(outPath);
                    System.IO.File.Delete(outPath); // Cleanup the temp file
                    return File(outputBytes, "audio/mpeg", "adjustedVolume.mp3");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
