node {
    stage('Clone repository') {
        git branch: 'main', credentialsId: 'github-app-IAmSilK', url: 'https://github.com/IAmSilK/NBCovidBot'
    }
    
    stage('Build image') {
        app = docker.build("nbcovidbot")
    }
    
    stage('Push image') {
        docker.withRegistry('http://127.0.0.1:6000') {
            app.push("1.0.${env.BUILD_NUMBER}")
            app.push('latest')
        }
    }
    
    stage('Deploy container') {
        sh '''
            docker ps -q --filter "name=nbcovidbot" | grep -q . && docker stop nbcovidbot
            docker ps -a -q --filter "name=nbcovidbot" | grep -q . && docker rm -fv nbcovidbot
            docker run -d -v nbcovidbot:/data --name nbcovidbot nbcovidbot:latest
        '''
    }
}