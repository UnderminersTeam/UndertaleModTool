import * as core from '@actions/core'
import * as artifact from '@actions/artifact'
import { resolve } from 'path'

async function run() {
    try {
        const names = core.getInput('names', { required: true }).split(' ');
        let groups = [ ];
        for(const group of names)
            groups.push(group.split(','));

        let groupDownloads = [ ];
        const artifactClient = artifact.create();
        for(const group of groups)
            groupDownloads.push(downloadGroup(artifactClient, group));
        await Promise.all(groupDownloads);

        core.info('Artifact download has finished successfully');
    }
    catch(error) {
        core.setFailed(error.message);
    }
}

async function downloadGroup(artifactClient, artifacts) {
    for(const name of artifacts) {
        core.info(`Starting download for ${name}`);
        const downloadOptions = { createArtifactFolder: true };
        const downloadResponse = await artifactClient.downloadArtifact(name, resolve('./'), downloadOptions);
        core.info(`Artifact ${downloadResponse.artifactName} was downloaded to ${downloadResponse.downloadPath}`);
    }
}

run();
