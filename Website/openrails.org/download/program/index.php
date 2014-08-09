﻿<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Download > Program</h1>
        <p>&nbsp;</p>
			</div>
		</div>
		<div class="row">
			<div class="col-md-1"></div>
			<div class="col-md-4">
			  <h1>Simple Download <small>(recommended)</small></h1>
        <br />
<?php 
$file = 'setup_OR_pre-v1.0_from_download.exe';
$exe = 'exe';
$file_path = "../../files";
echo "<a href='download.php?filepath=$file_path&file=$file&ext=$exe' class='btn download_button btn-lg btn-block'>";        
?>
          <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download the installer</h2>
          <p>
            This installer provides any missing pre-requisites for OR and an uninstaller too.
          </p>
        </a>
        <p style="text-align: center;">
<?php 
echo 'Date: ' . date('d-M-Y', filemtime("$file_path/$file")) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize("$file_path/$file") / 1024 / 1024) . 'MB'; ?>
        </p>
			</div>
			<div class="col-md-2"></div>
			<div class="col-md-4">
			  <h1>Supporters' Download</h1>
        <br>
<?php 
$file = 'OR_X.zip';
$exe = 'zip';
$file_path = "../../files";
echo "<a href='download.php?filepath=$file_path&file=$file&ext=$exe' class='btn download_button btn-lg btn-block'>";        
?>
          <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download just Open Rails</h2>
          <p>
            You must download and install any missing pre-requisites - see installation guides below.
          </p>
        </a>
        <p style="text-align: center;">
<?php 
echo 'Date: ' . date('d-M-Y', filemtime("$file_path/$file")) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize("$file_path/$file") / 1024 / 1024) . 'MB'; ?>
        </p>
        <p>
          This build of OR contains an easy, semi-automatic updater.
        </p>
			</div>
		</div>
 		<div class="row">
			<div class="col-md-6">
        <h2><span id="installation_questions">Installation Questions </span><small>from the FAQ</small></h2>
<?php include "../../learn/faq/install.php" ?>
      </div>
			<div class="col-md-6">
        <h2>Recent Code Changes</h2>
		<ul>
			<?php include "../../scripts/experimental_changelog1.html" ?>
		</ul>
		<p><a href='../changes/'>See more code changes</a></p>
        <h2>Installation Guides</h2>
  <ul>
    <li><a href="/files/installation_guide_en.pdf">Installation Guide (English)</a></li>
    <li><a href="/files/installation_guide_es.pdf">Tutorial de Instalación (Spanish)</a></li>
  </ul>
<p>
Note: Open Rails does not have to be installed into the Windows Registry and multiple versions can co-exist in different folders too. 
</p>
<h2>Versions and Updater</h2>
<p>
The updater checks this website for the arrival of a new update (usually Friday around 7pm London time).
</p>
<h3>Supporter's Update Setting</h3>
<p>
The updater can be set to check for the latest weekly version. Supporters may prefer this to get changes as soon as possible.
</p>
<h3>Safe Update Setting</h3>
<p>
By default, the updater is set to check for a version that is one week behind the latest. Occasionally, a new version may cause problems so this delay gives the team a chance to fix or withdraw any problem versions. 
</p>
			</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
