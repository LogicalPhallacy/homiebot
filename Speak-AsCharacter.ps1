[CmdletBinding()]
param (
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet(
        "craig-ferguson"
        ,"gilbert-gottfried"
        ,"sam-altman"
        ,"david-attenborough"
        ,"alan-rickman"
        ,"bart-simpson"
        ,"betty-white"
        ,"bill-gates"
        ,"bill-nye"
        ,"bryan-cranston"
        ,"christopher-lee"
        ,"danny-devito"
        ,"dr-phil-mcgraw"
        ,"j-k-simmons"
        ,"judi-dench"
        ,"leonard-nimoy"
        ,"neil-degrasse-tyson"
        ,"palmer-luckey"
        ,"peter-thiel"
        ,"richard-nixon"
        ,"jimmy-carter"
        ,"ronald-reagan"
        ,"bill-clinton"
        ,"george-w-bush"
        ,"shohreh-aghdashloo"
        ,"wilford-brimley"
        ,"anderson-cooper"
        ,"ben-stein"
        ,"david-cross"
        ,"hillary-clinton"
        ,"james-earl-jones"
        ,"john-oliver"
        ,"larry-king"
        ,"mark-zuckerberg"
        ,"fred-rogers"
        ,"sarah-palin"
        ,"tupac-shakur"
        ,"arnold-schwarzenegger"
        ,"george-takei"
        ,"paul-graham"
        ,"barack-obama"
    )]
    [string]$SpeakerName,
    [Parameter(Position = 1, Mandatory = $true)]
    [string]$Text
)

begin {
    $SoundPlayer = [System.Media.SoundPlayer]::new()
    $Headers = @{
        "Accept"="application/json"
        "User-Agent"="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36 Edg/84.0.522.52"
        "Origin"="https://vo.codes"
        "Sec-Fetch-Site"="cross-site"
        "Sec-Fetch-Mode"="cors"
        "Sec-Fetch-Dest"="empty"
        "Referer"="https://vo.codes/"
        "Accept-Encoding"="gzip, deflate, br"
        "Accept-Language"="en-US,en;q=0.9"
    }
}

process {
    Write-Host "Submitting Request, this may take a while"
    $Body = @{
        text = $Text
        speaker = $SpeakerName
    } | Convertto-Json
    $Response = Invoke-WebRequest -Uri "https://mumble.stream/speak_spectrogram" `
    -Method "POST" -Headers $Headers -ContentType "application/json" -Body $Body
    if($Response.StatusCode -ne [System.Net.HttpStatusCode]::OK){
        Write-Error "Unacceptable Response"
        return $Response
    }
    $AudioData = $Response | ConvertFrom-Json
}

end {
    Write-Host "Playing Audio"
    $MemStream = [System.IO.MemoryStream]::new([Convert]::FromBase64String($AudioData.audio_base64))
    $SoundPlayer.Stream = $MemStream
    $SoundPlayer.Play()
}