namespace MiniX.Emailer.Gen;

public class EmailGenerator
{
    public string GenerarMail2fa(string emailUsuario, string pin)
    {
        var msg = $"""
        <!doctype html>
        <html>
          <body>
            <div
              style='background-color:#000000;color:#FFFFFF;font-family:"Iowan Old Style", "Palatino Linotype", "URW Palladio L", P052, serif;font-size:16px;font-weight:400;letter-spacing:0.15008px;line-height:1.5;margin:0;padding:32px 0;min-height:100%;width:100%'
            >
              <table
                align="center"
                width="100%"
                style="margin:0 auto;max-width:600px;background-color:#000000"
                role="presentation"
                cellspacing="0"
                cellpadding="0"
                border="0"
              >
                <tbody>
                  <tr style="width:100%">
                    <td>
                      <div
                        style="color:#ffffff;font-size:14px;font-weight:normal;text-align:center;padding:16px 24px 16px 24px"
                      >
                        Aqui esta su codigo OTP:
                      </div>
                      <h1
                        style='font-weight:bold;text-align:center;margin:0;font-family:"Nimbus Mono PS", "Courier New", "Cutive Mono", monospace;font-size:32px;padding:16px 24px 16px 24px'
                      >
                        {pin}
                      </h1>
                      <div
                        style="color:#868686;font-size:10px;font-weight:normal;text-align:center;padding:16px 24px 16px 24px"
                      >
                        Este codigo es del usuario: {emailUsuario}
                      </div>
                      <div
                        style="color:#868686;font-size:10px;font-weight:normal;text-align:center;padding:16px 24px 16px 24px"
                      >
                        Si no sabes para que es el email, ignoralo.
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </body>
        </html>

        """;

        return msg;
    }
}
