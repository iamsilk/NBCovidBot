node {
    stage('Clone repository') {
        git branch: 'main', url: 'https://github.com/IAmSilK/NBCovidBot'
    }

    stage('Pull container') {
        sh '''
            docker pull ghcr.io/iamsilk/nbcovidbot:latest
        '''
    }

    stage('Deploy container') {
        sh '''
            docker-compose up -d
        '''
    }
}