#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Drawing"

using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;
using ImageResizer;

// Parameters...
static string API_KEY = Environment.GetEnvironmentVariable("VISION_API_KEY");
static string API_ENDPOINT = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/analyze?visualFeatures=Categories,Tags,Description,Faces,ImageType,Color";

public static void Run(CloudBlockBlob triggerBlob, Stream outputBlob, TraceWriter log)
{  
    log.Info($"### Triggered function on new blob: {triggerBlob.Name}. KEY {API_KEY}");

    // Build simple JSON request containing blob URL and POST to Computer Vision API
    // - Note, we just pass the URL of the blob (image file) to the API
    dynamic request = new {url = triggerBlob.Uri.ToString()};
    var api_resp = callCognitiveServiceApi(request, log);

    if(api_resp.message != null) {
        log.Error($"### !ERROR! {api_resp.message}");
        return;
    }
    // Grab values from API response
    var caption = api_resp.description.captions[0].text.ToString();
    caption = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(caption.ToLower());
    log.Info($"### Photo caption: '{caption}'");

    // Download blob as stream
    MemoryStream imgstream = new MemoryStream();
    triggerBlob.DownloadToStream(imgstream);
    Image original = Image.FromStream(imgstream);

    if(api_resp.faces.Count > 0) {
        foreach(var face in api_resp.faces) {
            drawFace(original, face);
        }   
    } 

    // Resize stream as a JPEG image and return result as Image
    var settings = new ImageResizer.ResizeSettings { MaxWidth = 2000, Width = 2000, Scale = ScaleMode.Both, Format = "jpg" };
    imgstream.Seek(0, SeekOrigin.Begin);
    Image resized_image = ImageResizer.ImageBuilder.Current.Build(original, settings);

    // Draw description as caption on the Image
    drawCaption(resized_image, caption, api_resp.tags);
    
    log.Info($"### Saving image to new blob");
    resized_image.Save(outputBlob, System.Drawing.Imaging.ImageFormat.Jpeg);
}


//
// Render caption as text on top of given image
//
public static void drawCaption(Image image, string caption, dynamic tags)
{
    float scale = (image.Width) / 4000.0F;
    string tag_text = "";
    for(var t = 0; t < Math.Min(tags.Count, 5); t++) {
        var tag_name = tags[t].name;
        var tag_conf = Math.Round((double)tags[t].confidence * 100);
        tag_text += $"\n - {tag_name} {tag_conf}%";
    } 

    using(Graphics g = Graphics.FromImage(image)) {
        using (Font arialFont = new Font("Segoe UI", 130F*scale, FontStyle.Bold, GraphicsUnit.Pixel)) {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.DrawString(caption, arialFont, Brushes.Black, new PointF(6F*scale, 6F*scale));
            g.DrawString(caption, arialFont, Brushes.White, new PointF(0f, 0f));
        }
        using (Font arialFont = new Font("Segoe UI", 90F*scale, FontStyle.Bold, GraphicsUnit.Pixel)) {
            g.DrawString(tag_text, arialFont, Brushes.Black, new PointF(6F*scale, 100F*scale+6F*scale));
            g.DrawString(tag_text, arialFont, Brushes.DarkOrange, new PointF(0f, 100F*scale));
        }
    }
}


//
// Draw box round face with age and gender
//
public static void drawFace(Image image, dynamic face)
{
    using(Graphics g = Graphics.FromImage(image)) {
        // magic scaling factor
        float scale = (image.Width) / 4000.0F;

        // Draw box round the face
        var facerect = face.faceRectangle;
        Rectangle rect = new Rectangle((int)facerect.left, (int)facerect.top, (int)facerect.width, (int)facerect.height);
        g.DrawRectangle(new Pen(Color.LimeGreen, scale*19F), rect);

        // Gibberish to position the caption above the box around the face
        string face_caption = face.gender+" ("+face.age.ToString()+")";
        PointF caption_p = new PointF((float)facerect.left-30F*scale, (float)facerect.top - (150F*scale));
        PointF caption_shadow_p = new PointF((float)facerect.left-30F*scale + (5F*scale), (float)facerect.top - ((150F*scale)-(5F*scale)));
        using (Font arialFont = new Font("Segoe UI", scale*90F, FontStyle.Bold, GraphicsUnit.Pixel)) {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.DrawString(face_caption, arialFont, Brushes.Black, caption_shadow_p);
            g.DrawString(face_caption, arialFont, Brushes.White, caption_p);
        }        
    }
}


//
// Simple HTTP POST call and JSON convert results 
//
public static dynamic callCognitiveServiceApi(dynamic request_obj, TraceWriter log)
{
    var client = new HttpClient();
    var content = new StringContent(JsonConvert.SerializeObject(request_obj), System.Text.Encoding.UTF8, "application/json");
    
    content.Headers.Add("Ocp-Apim-Subscription-Key", API_KEY);
    var resp = client.PostAsync(API_ENDPOINT, content).Result;
    dynamic resp_obj = JsonConvert.DeserializeObject( resp.Content.ReadAsStringAsync().Result );
    return resp_obj;
}