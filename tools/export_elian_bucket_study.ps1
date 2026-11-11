param(
    [string]$Frames = 'Library/ElianBucketStudyGrounded',
    [string]$Output = 'docs/previews/elian-bucket',
    [string]$Ffmpeg = 'ffmpeg'
)
$ErrorActionPreference='Stop'
foreach($direction in @('rechts','links')) {
    foreach($number in 0..336) {
        $frame=Join-Path $Frames ($direction+'/'+$number.ToString('0000')+'.png')
        if(!(Test-Path -LiteralPath $frame)) { throw "Missing captured Unity frame: $frame" }
    }
}
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$normal=Join-Path $Output 'Elian-Eimer-Normaltempo.mp4'
$slow=Join-Path $Output 'Elian-Eimer-Zeitlupe.mp4'
& $Ffmpeg -hide_banner -loglevel error -y -framerate 24 -i (Join-Path $Frames 'rechts/%04d.png') -framerate 24 -i (Join-Path $Frames 'links/%04d.png') -filter_complex '[0:v][1:v]concat=n=2:v=1:a=0,format=yuv420p[v]' -map '[v]' -c:v libx264 -preset medium -crf 18 -movflags +faststart $normal
if($LASTEXITCODE -ne 0) { throw 'Normal-speed video export failed.' }
& $Ffmpeg -hide_banner -loglevel error -y -i $normal -vf 'setpts=2*PTS' -r 24 -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -movflags +faststart $slow
if($LASTEXITCODE -ne 0) { throw 'Slow-motion video export failed.' }
& $Ffmpeg -hide_banner -loglevel error -y -framerate 24 -i (Join-Path $Frames 'rechts/%04d.png') -vf "select='eq(n,0)+eq(n,48)+eq(n,78)+eq(n,120)+eq(n,188)+eq(n,228)+eq(n,274)+eq(n,320)',scale=640:360,tile=2x4" -frames:v 1 (Join-Path $Output 'Schluesselposen.jpg')
if($LASTEXITCODE -ne 0) { throw 'Pose overview export failed.' }
@{
    scene='Assets/Scenes/AnimationStudies/ElianBucketMotionStudy.unity'
    project_version='0.51.1'; proposed_next_commit_version='0.51.2'
    unity='6000.3.26f1'; capture_fps=24; frames_per_direction=337
    directions=@('rechts','links'); slow_motion_speed=0.5
    normal_sha256=(Get-FileHash -LiteralPath $normal -Algorithm SHA256).Hash.ToLower()
    slow_sha256=(Get-FileHash -LiteralPath $slow -Algorithm SHA256).Hash.ToLower()
    standalone_build_created=$false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'INFO.json') -Encoding utf8
Get-Item -LiteralPath $normal,$slow | Select-Object Name,Length
