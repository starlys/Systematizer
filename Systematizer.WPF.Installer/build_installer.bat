set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin
cd \stage\SystematizerPublish
candle \dev\Systematizer\Systematizer.WPF.Installer\Product.wxs
light -ext WixUIExtension Product.wixobj
pause
