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
            # Stop nbcovidbot docker if it's running
            docker ps -q --filter "name=nbcovidbot" | grep -q . && docker stop nbcovidbot

            # Remove nbcovidbot docker if it exists
            docker ps -a -q --filter "name=nbcovidbot" | grep -q . && docker rm -fv nbcovidbot

            # Create nbcovidbot network if it doesn't exist
            docker network ls -q --filter "name=nbcovidbot" | grep -q . || docker network create nbcovidbot

            # Create and start nbcovidbot container
            docker run -d -v nbcovidbot:/data --network nbcovidbot --name nbcovidbot nbcovidbot:latest
        '''
    }

    stage('Connect MariaDb container to network') {
        sh '''
            # Connect mariadb-main to network if not already connected
            docker network inspect nbcovidbot -f "{{ range .Containers }}{{.Name}} {{ end }}" | grep -q mariadb-main \
                || docker network connect nbcovidbot mariadb-main
        '''
    }
}