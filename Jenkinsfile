pipeline {
	agent any
  	options {
		timestamps()
		ansiColor 'xterm'
	}

	stages {
		stage('Build Backend') {
			steps {
				buildDotnet('NinMemApi.sln', 'FolderProfile')
				archive 'PublishOutput/'
			}
		}

		stage('Deploy staging') {
			steps {
				deployWeb('\\\\it-webadbtest01.it.ntnu.no\\d$\\websites',
					"utv.artsdatabanken.no", '')
			}
		}
		stage("Smoke test staging") {
			steps {
				httpRequest 'http://it-webadbtest01.it.ntnu.no/ogapi/'
			}
		}
    stage("Manuell QA") {
			steps {
				timeout(time: 120, unit: 'SECONDS') {
          input('Bekreft at manuell verifikasjon av http://it-webadbtest01.it.ntnu.no/ogapi/ er godkjent og at du ønsker å rulle den ut i *PRODUKSJON*?')
				}
			}
    }
    stage("Deploy til it-webadb03") {
			steps {
				bat('dir')
				bat('dir PublishOutput\\')
				bat('dir PublishOutput\\OgApi\\')
				//delete('PublishOutput\\OgApi\\Web.config')
        deployWeb('\\\\it-webadb03.it.ntnu.no\\D$\\Websites\\database.artsdatabanken.no', 'database.artsdatabanken.no/', 'Drift')
			}
    }
    stage("Deploy til it-webadb04") {
			steps {
        deployWeb('\\\\it-webadb04.it.ntnu.no\\D$\\Websites\\database.artsdatabanken.no', 'database.artsdatabanken.no/', 'Drift')
			}
    }
    stage("Smoke test production") {
			steps {
        httpRequest 'https://database.artsdatabanken.no/ogapi/'
			}
    }
	}
	post {
		failure {
			emailext(
				subject: "FAILED: ${env.JOB_NAME} Build #${env.BUILD_NUMBER}!",
				to: emailextrecipients([
					[$class: 'CulpritsRecipientProvider'],
					[$class: 'RequesterRecipientProvider'],
					[$class: 'DevelopersRecipientProvider']
				]),
				body: """FAILED: <a href='${env.BUILD_URL}'>${env.BUILD_URL}</a>""",
				mimeType: 'text/html',
				replyTo: '$DEFAULT_REPLYTO',
				attachLog: true)
		}
		unstable {
			bat('echo This will run only if the run was marked as unstable')
		}
		changed {
			bat('echo Build status changed to ?')
		}
	}
}

def deployWeb(baseUnc, appName, release) {
		dir('PublishOutput\\OgApi' + release) { // api
			virtualPath = '/ogapi/' + env.BRANCH_NAME + '_api'
			physicalPath = baseUnc+'\\OgApi\\'+env.BRANCH_NAME+'_api'
			deployPath('.', physicalPath)
  			iisConvertToApplication(appName, virtualPath, physicalPath)
	}
}

def deployPath(src, dest) {
	mkdir(dest)
	bat 'xcopy "' + src + '" "' + dest + '" /s /y'
}

def deploy(src, dest) {
	mkdir(dest)
	bat 'xcopy "' + src + '" "' + dest + '" /s /y'
}

def mkdir(path) {
	bat 'if not exist ' + path + ' mkdir ' + path
}

def delete(filespec) {
	bat 'IF EXIST ' + filespec + ' DEL ' + filespec + ' /Q'
}

def buildDotnet(slnPath, publishProfile) {
	bat 'SUBST P: /D | ECHO' // Swallow exit code when subst does not exist
	bat 'SUBST P: ' + WORKSPACE
	bat 'NuGet.exe restore "' + 'P:' + slnPath + '"'
	bat 'SUBST P: /D | ECHO'
	bat '"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\BuildTools\\MSBuild\\15.0\\bin\\msbuild.exe" '+slnPath+
	' /p:Configuration=Release /p:Platform="Any CPU" /verbosity:quiet '+
	'/p:ProductVersion=1.0.0.${env.BUILD_NUMBER} /p:DeployOnBuild=true '+
	'/p:PublishProfile=' + publishProfile
}