/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { ChangeDetectorRef, Component, ElementRef, Input, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { MentionConfig } from 'angular-mentions';
import { timer } from 'rxjs';
import { switchSafe } from '@app/framework';
import { AppsState, AuthService, CommentDto, CommentsService, CommentsState, ContributorsState, DialogService, ResourceOwner, UpsertCommentForm } from '@app/shared/internal';
import { CommentComponent } from './comment.component';

@Component({
    selector: 'sqx-comments',
    styleUrls: ['./comments.component.scss'],
    templateUrl: './comments.component.html',
})
export class CommentsComponent extends ResourceOwner {
    @ViewChild('commentsList', { static: false })
    public commentsList!: ElementRef<HTMLDivElement>;

    @ViewChildren(CommentComponent)
    public children!: QueryList<CommentComponent>;

    @Input()
    public commentsId = '';

    public commentsUrl!: string;
    public commentsState!: CommentsState;
    public commentForm = new UpsertCommentForm();

    public mentionUsers = this.contributorsState.contributors;
    public mentionConfig: MentionConfig = { dropUp: true, labelKey: 'contributorEmail' };

    public userToken = '';

    constructor(authService: AuthService,
        private readonly appsState: AppsState,
        private readonly commentsService: CommentsService,
        private readonly contributorsState: ContributorsState,
        private readonly changeDetector: ChangeDetectorRef,
        private readonly dialogs: DialogService,
        private readonly router: Router,
    ) {
        super();

        this.userToken = authService.user!.token;
    }

    public ngOnChanges() {
        this.contributorsState.load();

        this.commentsUrl = `apps/${this.appsState.appName}/comments/${this.commentsId}`;
        this.commentsState = new CommentsState(this.commentsUrl, this.commentsService, this.dialogs);

        this.own(timer(0, 4000).pipe(switchSafe(() => this.commentsState.load(true))));
    }

    public scrollDown() {
        if (this.commentsList && this.commentsList.nativeElement) {
            let isEditing = false;

            this.children.forEach(x => {
                isEditing = isEditing || x.snapshot.isEditing;
            });

            if (!isEditing) {
                const height = this.commentsList.nativeElement.scrollHeight;

                this.commentsList.nativeElement.scrollTop = height;
            }
        }
    }

    public comment() {
        const value = this.commentForm.submit();

        if (value && value.text && value.text.length > 0) {
            this.commentsState.create(value.text, this.router.url);

            this.changeDetector.detectChanges();
        }

        this.commentForm.submitCompleted();
    }

    public trackByComment(_index: number, comment: CommentDto) {
        return comment.id;
    }
}
