pipeline {
  agent {
    node {
      label 'senko-dev'
    }
  }
  stages {
    stage('Build') {
      steps {
        sh 'dotnet build Miki/Miki.sln --configuration "Debug Production"'
      }
    }
    stage('Update SenkoDev') {
      when {
        branch 'senko';
      }
      steps {
        discordSend description: 'A new commit has been made on GitHub so we\'re updating SenkoDev to the latest version. Please wait a minute.', title: 'SenkoDev is being updated...', webhookURL: 'https://discordapp.com/api/webhooks/580143073087193098/3Xzo6Lw2MEmj3euH0qv0GiJHsE-MURRBkF484tHQdMgsMTJXK-NTCrD93zx73Br7ksxV'
        sh 'bash /home/jenkins/update.sh'
      }
    }
  }
}
